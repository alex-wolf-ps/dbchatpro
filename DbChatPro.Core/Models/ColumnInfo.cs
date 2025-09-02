namespace DBChatPro.Models
{
    public class ColumnInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        
        public string DisplayName => string.IsNullOrEmpty(DataType) ? Name : $"{Name} ({DataType})";
    }
}