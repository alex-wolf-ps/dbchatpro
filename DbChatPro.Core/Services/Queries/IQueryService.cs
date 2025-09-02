using DBChatPro.Models;

namespace DBChatPro
{
    public interface IQueryService
    {
        Task<List<HistoryItem>> GetQueries(string connectionName, QueryType queryType);
        Task SaveQuery(string query, string connectionName, QueryType queryType);
        Task SaveQuery(string query, string connectionName, QueryType queryType, string customName, string tags);
    }
}