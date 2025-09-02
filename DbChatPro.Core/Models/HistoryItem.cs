using Azure;
using Azure.Data.Tables;

namespace DBChatPro.Models
{
    public class HistoryItem : ITableEntity
    {
        public int Id { get; set; }
        public string Query { get; set; }
        public string Name { get; set; }
        public string ConnectionName { get; set; }
        public QueryType QueryType { get; set; }
        public string Tags { get; set; } = string.Empty;
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }

    public enum QueryType
    {
        Favorite,
        History
    }
}
