using Azure;
using Azure.Data.Tables;
using DBChatPro.Models;

namespace DBChatPro
{
    public class AzureTableQueryService(TableServiceClient tableServiceClient) : IQueryService
    {
        public async Task SaveQuery(string query, string connectionName, QueryType queryType)
        {
            TableClient client = tableServiceClient.GetTableClient(
                tableName: "queries"
            );

            var entity = new HistoryItem()
            {
                Id = new Random().Next(0, 10000),
                Query = query,
                Name = query,
                ConnectionName = connectionName,
                QueryType = queryType,
                RowKey = Guid.NewGuid().ToString(),
                PartitionKey = Enum.GetName(queryType),
                Timestamp = DateTime.Now
            };

            Response response = await client.UpsertEntityAsync<HistoryItem>(
                entity: entity,
                mode: TableUpdateMode.Replace
            );
        }

        public async Task<List<HistoryItem>> GetQueries(string connectionName, QueryType queryType)
        {
            TableClient client = tableServiceClient.GetTableClient(
                tableName: "queries"
            );

            string category = Enum.GetName(queryType);

            AsyncPageable<HistoryItem> results = client.QueryAsync<HistoryItem>(
                product => product.PartitionKey == category
            );

            List<HistoryItem> entities = new();
            await foreach (HistoryItem product in results)
            {
                if (product.QueryType == queryType)
                {
                    entities.Add(product);
                }
            }

            return entities;
        }
    }
}
