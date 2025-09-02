using DBChatPro.Models;
using Snowflake.Data.Client;

namespace DBChatPro
{
    // AI-generated: Snowflake database service implementation
    // Follows the same pattern as other database services in the application
    public class SnowflakeDatabaseService : IDatabaseService
    {
        public async Task<List<List<string>>> GetDataTable(AIConnection conn, string sqlQuery)
        {
            var rows = new List<List<string>>();
            
            using var connection = new SnowflakeDbConnection(conn.ConnectionString);
            await connection.OpenAsync();

            using var command = new SnowflakeDbCommand(connection)
            {
                CommandText = sqlQuery
            };
            using var reader = await command.ExecuteReaderAsync();

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
                        var value = reader.GetValue(i);
                        cols.Add(value?.ToString() ?? "NULL");
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
            List<KeyValuePair<string, string>> rows = new();

            // Extract database and schema from connection string if possible
            // Snowflake connection format typically includes account, warehouse, database, schema
            var pairs = conn.ConnectionString.Split(";");
            var database = pairs.Where(x => x.ToLower().Contains("db=") || x.ToLower().Contains("database="))
                               .FirstOrDefault()?.Split("=").Last() ?? "PUBLIC";
            var schema = pairs.Where(x => x.ToLower().Contains("schema="))
                             .FirstOrDefault()?.Split("=").Last() ?? "PUBLIC";

            // Query to get table and column information from Snowflake's information schema
            string sqlQuery = $@"SELECT 
                                    table_name, 
                                    column_name 
                                FROM 
                                    information_schema.columns 
                                WHERE 
                                    table_catalog = CURRENT_DATABASE()
                                    AND table_schema = '{schema.ToUpper()}'
                                ORDER BY 
                                    table_name, 
                                    column_name;";

            using var connection = new SnowflakeDbConnection(conn.ConnectionString);
            await connection.OpenAsync();

            using var command = new SnowflakeDbCommand(connection)
            {
                CommandText = sqlQuery
            };
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var tableName = reader.GetString(0);
                var columnName = reader.GetString(1);
                rows.Add(new KeyValuePair<string, string>(tableName, columnName));
            }

            var groups = rows.GroupBy(x => x.Key);

            foreach (var group in groups)
            {
                dbSchema.SchemaStructured.Add(new TableSchema() { TableName = group.Key, Columns = group.Select(x => x.Value).ToList() });
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

            return dbSchema;
        }
    }
}