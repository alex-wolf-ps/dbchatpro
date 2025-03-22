namespace DBChatPro.Models
{
    public class DatabaseSchema
    {
        public List<TableSchema> SchemaStructured { get; set; }
        public List<string> SchemaRaw { get; set; }
    }
}
