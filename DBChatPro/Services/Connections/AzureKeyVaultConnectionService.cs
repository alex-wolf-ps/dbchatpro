
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace DBChatPro.Services
{
    public class AzureKeyVaultConnectionService(SecretClient secretClient) : IConnectionService
    {
        public async Task AddConnection(AIConnection connection)
        {
            await secretClient.SetSecretAsync(connection.Name, connection.ConnectionString);
        }

        public async Task DeleteConnection(string name)
        {
            var operation = await secretClient.StartDeleteSecretAsync(name);

            // You only need to wait for completion if you want to purge or recover the key.
            await operation.WaitForCompletionAsync();

            await secretClient.PurgeDeletedSecretAsync(name);
        }

        public async Task<List<AIConnection>> GetAIConnections()
        {
            var connections = new List<AIConnection>();

            await foreach (var page in secretClient.GetPropertiesOfSecretsAsync().AsPages())
            {
                foreach (var secret in page.Values)
                {
                    var secretValue = await secretClient.GetSecretAsync(secret.Name);
                    connections.Add(new AIConnection() { Name = secret.Name, ConnectionString = secretValue.Value.Value });
                }
            }

            return connections;
        }
    }
}
