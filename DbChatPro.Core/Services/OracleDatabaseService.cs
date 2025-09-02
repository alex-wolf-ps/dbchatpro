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
            List<(string TableName, string ColumnName, string DataType)> rows = new();

            using (OracleConnection con = new OracleConnection(conn.ConnectionString))
            {
                using (OracleCommand command = con.CreateCommand())
                {
                    con.Open();

                    command.CommandText = @"SELECT table_name, 
                           column_name,
                           CASE 
                               WHEN data_type IN ('VARCHAR2', 'CHAR', 'NVARCHAR2', 'NCHAR') THEN data_type || '(' || data_length || ')'
                               WHEN data_type = 'NUMBER' AND data_precision IS NOT NULL THEN 
                                   CASE WHEN data_scale > 0 THEN data_type || '(' || data_precision || ',' || data_scale || ')'
                                        ELSE data_type || '(' || data_precision || ')'
                                   END
                               ELSE data_type
                           END as data_type_formatted
                        FROM user_tab_columns
                        ORDER BY table_name, column_id";

                    using (OracleDataReader reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                    {
                        rows.Add((reader.GetValue(0).ToString() ?? "", reader.GetValue(1).ToString() ?? "", reader.GetValue(2).ToString() ?? ""));
                    }
                }
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

            return dbSchema;
        }
    }
}
