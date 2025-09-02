using DBChatPro.Models;
using Microsoft.Data.SqlClient;
using MySqlConnector;

namespace DBChatPro
{
    public class MySqlDatabaseService : IDatabaseService
    {
        public async Task<List<List<string>>> GetDataTable(AIConnection conn, string sqlQuery)
        {
            var rows = new List<List<string>>();
            MySqlConnection connection = new MySqlConnection(conn.ConnectionString);

            await connection.OpenAsync();

            using var command = new MySqlCommand(sqlQuery, connection);
            using var reader = await command.ExecuteReaderAsync();

            int count = 0;
            bool headersAdded = false;
            while (await reader.ReadAsync())
            {
                var cols = new List<string>();
                var headerCols = new List<string>();
                if (!headersAdded)
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        headerCols.Add(reader.GetName(i).ToString());
                    }
                    headersAdded = true;
                    rows.Add(headerCols);
                }

                for (int i = 0; i <= reader.FieldCount - 1; i++)
                {
                    try
                    {
                        cols.Add(reader.GetValue(i).ToString());
                    }
                    catch
                    {
                        cols.Add("DataTypeConversionError");
                    }
                }
                rows.Add(cols);
            }

            return rows;
        }

        public async Task<DatabaseSchema> GenerateSchema(AIConnection conn)
        {
            var dbSchema = new DatabaseSchema() { SchemaRaw = new List<string>(), SchemaStructured = new List<TableSchema>() };
            List<(string TableName, string ColumnName, string DataType)> rows = new();

            var pairs = conn.ConnectionString.Split(";");
            var database = pairs.Where(x => x.Contains("Database")).FirstOrDefault().Split("=").Last();

            string sqlQuery = $@"SELECT 
                                    TABLE_NAME, 
                                    COLUMN_NAME,
                                    CASE 
                                        WHEN DATA_TYPE IN ('varchar', 'char', 'text') AND CHARACTER_MAXIMUM_LENGTH IS NOT NULL THEN CONCAT(DATA_TYPE, '(', CHARACTER_MAXIMUM_LENGTH, ')')
                                        WHEN DATA_TYPE IN ('decimal', 'numeric') AND NUMERIC_PRECISION IS NOT NULL THEN CONCAT(DATA_TYPE, '(', NUMERIC_PRECISION, ',', NUMERIC_SCALE, ')')
                                        ELSE DATA_TYPE
                                    END as DATA_TYPE_FORMATTED
                                FROM 
                                    INFORMATION_SCHEMA.COLUMNS 
                                WHERE 
                                    TABLE_SCHEMA = '{database}'
                                ORDER BY TABLE_NAME, ORDINAL_POSITION;";

            MySqlConnection connection = new MySqlConnection(conn.ConnectionString);

            connection.Open();

            await using (var command = new MySqlCommand(sqlQuery, connection))
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    rows.Add((reader.GetValue(0).ToString() ?? "", reader.GetValue(1).ToString() ?? "", reader.GetValue(2).ToString() ?? ""));
                }

                var groups = rows.GroupBy(x => x.TableName);

                foreach (var group in groups)
                {
                    var tableSchema = new TableSchema() 
                    { 
                        TableName = group.Key,
                        ColumnInfos = group.Select(x => new DBChatPro.Models.ColumnInfo { Name = x.ColumnName, DataType = x.DataType }).ToList()
                    };
                    dbSchema.SchemaStructured.Add(tableSchema);
                }

                var textLines = new List<string>();

                foreach (var table in dbSchema.SchemaStructured)
                {
                    var schemaLine = $"- {table.TableName} (";

                    foreach (var column in table.Columns)
                    {
                        schemaLine += column + ", ";
                    }

                    schemaLine += ")";
                    schemaLine = schemaLine.Replace(", )", " )");

                    Console.WriteLine(schemaLine);
                    textLines.Add(schemaLine);
                }

                dbSchema.SchemaRaw = textLines;

            }

            return dbSchema;
        }
    }
}
