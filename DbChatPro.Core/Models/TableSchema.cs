using DBChatPro.Models;

namespace DBChatPro
{
    public class TableSchema()
    {
        public string TableName { get; set; } = string.Empty;
        public List<ColumnInfo> ColumnInfos { get; set; } = new();
        
        // Maintain backward compatibility
        public List<string> Columns => ColumnInfos.Select(c => c.DisplayName).ToList();
    }
}
