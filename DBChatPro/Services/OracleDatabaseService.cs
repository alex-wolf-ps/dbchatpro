using DBChatPro.Models;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Text;
using System.Text.Json;

namespace DBChatPro
{
    public class OracleDatabaseService : IDatabaseService
    {
        public async Task<List<List<string>>> GetDataTable(AIConnection conn, string sqlQuery)
        {
            var rows = new List<List<string>>();
            using (OracleConnection connection = new OracleConnection(conn.ConnectionString))
            {
                using var command = new OracleCommand(sqlQuery, connection);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                int count = 0;
                bool headersAdded = false;
                if (reader.HasRows){
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
                }
            }

            return rows;
        }

        public async Task<DatabaseSchema> GenerateSchema(AIConnection conn)
        {
            var dbSchema = new DatabaseSchema() { SchemaRaw = new List<string>(), SchemaStructured = new List<TableSchema>() };
            List<KeyValuePair<string, string>> rows = new();

            using (OracleConnection con = new OracleConnection(conn.ConnectionString))
            {
                using (OracleCommand command = con.CreateCommand())
                {
                    con.Open();

                    command.CommandText = @"SELECT *
                        FROM user_tab_columns
                        ORDER BY table_name, column_id";

                    using (OracleDataReader reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                    {
                        rows.Add(new KeyValuePair<string, string>(reader.GetValue(0).ToString(), reader.GetValue(1).ToString()));
                    }
                }
            }

            var groups = rows.GroupBy(x => x.Key);

            foreach (var group in groups)
            {
                dbSchema.SchemaStructured.Add(new TableSchema() { TableName = group.Key, Columns = group.Select(x => x.Value).ToList() });
                //use this list
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
