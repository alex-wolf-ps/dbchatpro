using Amazon.BedrockRuntime;
using Azure.AI.OpenAI;
using Azure.Identity;
using DBChatPro;
using DBChatPro.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Azure;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using MudBlazor;
using MudBlazor.Services;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped<AIService>();
builder.Services.AddScoped<IDatabaseService, DatabaseManagerService>();
builder.Services.AddScoped<MySqlDatabaseService>();
builder.Services.AddScoped<SqlServerDatabaseService>();
builder.Services.AddScoped<PostgresDatabaseService>();
builder.Services.AddScoped<OracleDatabaseService>();

if (!string.IsNullOrEmpty(builder.Configuration["AWS:Profile"]))
{
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
    builder.Services.AddAWSService<IAmazonBedrockRuntime>();
}

// For Azure OpenAI using Entra ID
#region Credential chain
// Build up credential chain for cloud and local tooling options
var userAssignedIdentityCredential = 
    new ManagedIdentityCredential(builder.Configuration.GetValue<string>("AZURE_CLIENT_ID"));
    
var visualStudioCredential = new VisualStudioCredential(
    new VisualStudioCredentialOptions()
    { 
        TenantId = builder.Configuration.GetValue<string>("AZURE_TENANT_ID") 
    });

var azureDevCliCredential = new AzureDeveloperCliCredential(
    new AzureDeveloperCliCredentialOptions()
    {
        TenantId = builder.Configuration.GetValue<string>("AZURE_TENANT_ID")
    });

var azureCliCredential = new AzureCliCredential(
    new AzureCliCredentialOptions()
    {
        TenantId = builder.Configuration.GetValue<string>("AZURE_TENANT_ID")
    });

var credential = new ChainedTokenCredential(userAssignedIdentityCredential, azureDevCliCredential, visualStudioCredential, azureCliCredential);
#endregion

// Use in-memory services in local mode
if (builder.Configuration["EnvironmentMode"] == "local")
{
    builder.Services.AddSingleton<IQueryService, InMemoryQueryService>();
    builder.Services.AddSingleton<IConnectionService, InMemoryConnectionService>();
}
// AZURE HOSTED ONLY FOR USE WITH AZURE DEVELOPER CLI - currently only supports hosting on Azure with Azure OpenAI, so use Azure services in hosted mode
else if (builder.Configuration["EnvironmentMode"] == "azure")
{
    var azureOpenAIEndpoint = new Uri(builder.Configuration["AZURE_OPENAI_ENDPOINT"]);
    var azureTableEndpoint = new Uri(builder.Configuration["AZURE_STORAGE_ENDPOINT"]);
    var azureKeyVaultEndpoint = new Uri(builder.Configuration["AZURE_KEYVAULT_ENDPOINT"]);

    builder.Services.AddAzureClients(async clientBuilder =>
    {
        // Register the table storage and key vault services
        clientBuilder.AddTableServiceClient(azureTableEndpoint);
        clientBuilder.AddSecretClient(azureKeyVaultEndpoint);

        // Comment this AddClient block out if you're using vanilla OpenAI instead of Azure OpenAI
        clientBuilder.AddClient<AzureOpenAIClient, AzureOpenAIClientOptions>(
            (options, _, _) => new AzureOpenAIClient(
                azureOpenAIEndpoint, credential, options)); // Replace "credential" with new ApiKeyCredential("your-key") to use key based auth with Azure

        clientBuilder.UseCredential(credential);
    });

    builder.Services.AddScoped<IQueryService, AzureTableQueryService>();
    builder.Services.AddScoped<IConnectionService, AzureKeyVaultConnectionService>();
}

// Mudblazor stuff
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;

    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<DBChatPro.UI.Components.App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

app.Run();
