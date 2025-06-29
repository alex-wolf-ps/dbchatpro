using System.Text.Json;

namespace DBChatPro.Services
{
    public interface IConnectionService
    {
        Task AddConnection(AIConnection connection);
        Task DeleteConnection(string name);
        Task<List<AIConnection>> GetAIConnections();
    }
}