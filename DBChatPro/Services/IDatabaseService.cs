using DBChatPro.Models;

namespace DBChatPro
{
    public interface IDatabaseService
    {
        Task<List<List<string>>> GetDataTable(AIConnection conn, string sqlQuery);
        Task<DatabaseSchema> GenerateSchema(AIConnection conn);
    }
}