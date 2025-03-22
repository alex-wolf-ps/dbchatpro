using DBChatPro.Models;

namespace DBChatPro
{
    public class InMemoryQueryService : IQueryService
    {
        private List<HistoryItem> queries = new();

        public Task<List<HistoryItem>> GetQueries(string connectionName, QueryType queryType)
        {
            return Task.FromResult(queries.Where(x => x.QueryType == queryType).ToList());
        }

        public Task SaveQuery(string query, string connectionName, QueryType queryType)
        {
            queries.Add(new HistoryItem()
            {
                Id = new Random().Next(0, 10000),
                Query = query,
                Name = query,
                ConnectionName = connectionName,
                QueryType = queryType
            });

            return Task.CompletedTask;
        }
    }
}
