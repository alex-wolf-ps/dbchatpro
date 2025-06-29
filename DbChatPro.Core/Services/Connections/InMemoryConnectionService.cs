using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DBChatPro.Services
{
    public class InMemoryConnectionService : IConnectionService
    {
        private List<AIConnection> connections = new();

        public async Task<List<AIConnection>> GetAIConnections()
        {
            return connections;
        }

        public async Task AddConnection(AIConnection connection)
        {
            connections.Add(connection);
        }

        public async Task DeleteConnection(string name)
        {
            var connection = connections.FirstOrDefault(x => x.Name == name);
            connections.Remove(connection);
        }
    }
}
