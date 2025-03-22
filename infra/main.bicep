targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name which is used to generate a short unique hash for each resource')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Id of the user or app to assign application roles')
param principalId string

var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
param openAiResourceLocation string
param openAiSkuName string = ''
param openAiApiVersion string = '' // Used by the SDK in the app code
param disableKeyBasedAuth bool = true

// Parameters for the specific Azure OpenAI deployment:
// Parameters for the Azure OpenAI resource:
param openAiResourceName string = ''
param openAiDeploymentName string // Set in main.parameters.json
param openAiModelName string // Set in main.parameters.json
param openAiModelVersion string // Set in main.parameters.json
param openAiDeploymentCapacity int // Set in main.parameters.json
param openAiDeploymentSkuName string // Set in main.parameters.json

var prefix = 'pro${resourceToken}' // 'pro' to make sure prefix starts with a letter

// Container app checks if exists
@description('Specifies if the resource already exists')
param exists bool = false

@description('The name of the container image')
param imageName string = ''

// Tags that should be applied to all resources.
// 
// Note that 'azd-service-name' tags should be applied separately to service host resources.
// Example usage:
//   tags: union(tags, { 'azd-service-name': <service name in azure.yaml> })
var tags = {
  'azd-env-name': environmentName
}

// Resource group to hold everything
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'rg-${environmentName}'
  location: location
}

// Identity that will be used by the app
module acaIdentity 'core/security/aca-identity.bicep' = {
  name: '${prefix}-aca-identity'
  params: {
    identityName: '${prefix}-aca-identity'
    location: location
  }
  scope:resourceGroup
}

// Hosting
module logAnalyticsWorkspace 'core/monitor/loganalytics.bicep' = {
  name: 'loganalytics'
  scope: resourceGroup
  params: {
    name: '${prefix}-loganalytics'
    location: location
  }
}

module containerAppsEnvironment 'core/host/container-apps-environment.bicep' = {
  name: '${prefix}-containerapps-env'
  scope: resourceGroup
  params: {
    name: '${prefix}-containerapps-env'
    location: location
    tags: tags
    logAnalyticsWorkspaceName: logAnalyticsWorkspace.outputs.name
  }
}

module containerRegistry 'core/host/container-registry.bicep' = {
  name: '${replace(prefix, '-', '')}registry'
  scope: resourceGroup
  params: {
    name: '${replace(prefix, '-', '')}registry'
    location: location
  }
}

var envVars = [
  {
    name: 'AZURE_OPENAI_CHATGPT_DEPLOYMENT'
    value: openAiDeploymentName
  }
  {
    name: 'AZURE_OPENAI_ENDPOINT'
    value: openAi.outputs.endpoint
  }
  {
    name: 'AZURE_STORAGE_ENDPOINT'
    value: storageAccount.outputs.endpoint
  }
  {
    name: 'AZURE_KEYVAULT_ENDPOINT'
    value: keyVault.outputs.endpoint
  }
  {
    name: 'AZURE_OPENAI_API_VERSION'
    value: openAiApiVersion
  }
  {
    name: 'RUNNING_IN_PRODUCTION'
    value: 'true'
  }
  {
    // DefaultAzureCredential will look for an environment variable with this name:
    name: 'AZURE_CLIENT_ID'
    value: acaIdentity.outputs.clientId
  }
  {
    // DefaultAzureCredential will look for an environment variable with this name:
    name: 'AZURE_TENANT_ID'
    value: subscription().tenantId
  }
]

module containerRegistryRoleUser 'core/security/role-assignment.bicep' =  {
  scope: resourceGroup
  name: 'acr-pull'
  params: {
    principalId: acaIdentity.outputs.principalId
    roleDefinitionId: '7f951dda-4ed3-4680-a7ca-43fe172d538d'
    principalType: 'ServicePrincipal'
  }
}

// Check if container app already exists for future deploys
resource existingApp 'Microsoft.App/containerApps@2023-05-02-preview' existing = if (exists) {
  scope: resourceGroup
  name: '${prefix}-app'
}

module containerApp 'core/host/container-app.bicep' = {
  name: '${deployment().name}-update'
  scope: resourceGroup
  params: {
    name: '${prefix}-app'
    location: location
    tags: union(tags, { 'azd-service-name': 'dbchatpro' })
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistry.name
    containerMaxReplicas: 1
    identityName: acaIdentity.outputs.identityName
    imageName: !empty(imageName) ? imageName : exists ? existingApp.properties.template.containers[0].image : ''
    env: envVars
    targetPort: 8080
  }
}

// Resources
module openAi 'core/ai/cognitiveservices.bicep' = {
  name: 'openai'
  scope: resourceGroup
  params: {
    name: !empty(openAiResourceName) ? openAiResourceName : '${resourceToken}-cog'
    location: !empty(openAiResourceLocation) ? openAiResourceLocation : location
    tags: tags
    disableLocalAuth: disableKeyBasedAuth
    sku: {
      name: !empty(openAiSkuName) ? openAiSkuName : 'S0'
    }
    deployments: [
      {
        name: openAiDeploymentName
        model: {
          format: 'OpenAI'
          name: openAiModelName
          version: openAiModelVersion
        }
        sku: {
          name: openAiDeploymentSkuName
          capacity: openAiDeploymentCapacity
        }
      }
    ]
  }
}

module keyVault 'core/security/keyvault.bicep' = {
  name: 'keyvault'
  scope: resourceGroup
  params: {
    keyVaultName: '${prefix}kv'
    location: location
    principalId: principalId
    tenantId: subscription().tenantId
    keysPermissions: [
      'list'
    ]
    secretsPermissions: [
      'list'
    ]
  }
}

module storageAccount 'core/storage/table.bicep' = {
  name: 'storage-account'
  scope: resourceGroup
  params: {
    storageAccountName: '${prefix}sa'
    location: location
  }
}

// Assign roles
module openAiRoleUser 'core/security/role-assignment.bicep' =  {
  scope: resourceGroup
  name: 'openai-role-user'
  params: {
    principalId: principalId
    roleDefinitionId: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
    principalType: 'User'
  }
}

module openAiRoleBackend 'core/security/role-assignment.bicep' = {
  scope: resourceGroup
  name: 'openai-role-backend'
  params: {
    principalId: acaIdentity.outputs.principalId
    roleDefinitionId: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
    principalType: 'ServicePrincipal'
  }
}

module storageRoleUser 'core/security/role-assignment.bicep' =  {
  scope: resourceGroup
  name: 'storage-role-user'
  params: {
    principalId: principalId
    roleDefinitionId: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
    principalType: 'User'
  }
}

module storageRoleBackend 'core/security/role-assignment.bicep' = {
  scope: resourceGroup
  name: 'storage-role-backend'
  params: {
    principalId: acaIdentity.outputs.principalId
    roleDefinitionId: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
    principalType: 'ServicePrincipal'
  }
}

module keyvaultRoleUser 'core/security/role-assignment.bicep' =  {
  scope: resourceGroup
  name: 'keyvault-role-user'
  params: {
    principalId: principalId
    roleDefinitionId: 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'
    principalType: 'User'
  }
}

module keyvaultRoleBackend 'core/security/role-assignment.bicep' = {
  scope: resourceGroup
  name: 'keyvault-role-backend'
  params: {
    principalId: acaIdentity.outputs.principalId
    roleDefinitionId: 'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'
    principalType: 'ServicePrincipal'
  }
}

// These get saved to .NET user secrets for the proejct
output AZURE_STORAGE_ENDPOINT string = storageAccount.outputs.endpoint
output AZURE_KEYVAULT_ENDPOINT string = keyVault.outputs.endpoint
output AZURE_OPENAI_ENDPOINT string = openAi.outputs.endpoint
output AZURE_CLIENT_ID string = acaIdentity.outputs.principalId
output AZURE_TENANT_ID string = subscription().tenantId

output AZURE_LOCATION string = location
output AZURE_OPENAI_CHATGPT_DEPLOYMENT string = openAiDeploymentName
output AZURE_OPENAI_API_VERSION string = openAiApiVersion

output SERVICE_ACA_IDENTITY_PRINCIPAL_ID string = acaIdentity.outputs.principalId
output SERVICE_ACA_NAME string = containerApp.outputs.name
output SERVICE_ACA_URI string = containerApp.outputs.uri
output SERVICE_ACA_IMAGE_NAME string = containerApp.outputs.imageName

output AZURE_CONTAINER_ENVIRONMENT_NAME string = containerAppsEnvironment.outputs.name
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.outputs.loginServer
output AZURE_CONTAINER_REGISTRY_NAME string = containerRegistry.name
