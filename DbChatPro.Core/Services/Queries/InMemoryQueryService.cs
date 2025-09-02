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
            return SaveQuery(query, connectionName, queryType, query, string.Empty);
        }

        public Task SaveQuery(string query, string connectionName, QueryType queryType, string customName, string tags)
        {
            queries.Add(new HistoryItem()
            {
                Id = new Random().Next(0, 10000),
                Query = query,
                Name = customName,
                ConnectionName = connectionName,
                QueryType = queryType,
                Tags = tags
            });

            return Task.CompletedTask;
        }
    }
}
