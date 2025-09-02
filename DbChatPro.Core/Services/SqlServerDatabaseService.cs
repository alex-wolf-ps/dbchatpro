using DBChatPro.Models;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Text.Json;

namespace DBChatPro
{
    public class SqlServerDatabaseService : IDatabaseService
    {
        public async Task<List<List<string>>> GetDataTable(AIConnection conn, string sqlQuery)
        {
            var rows = new List<List<string>>();
            using (SqlConnection connection = new SqlConnection(conn.ConnectionString))
            {
                using var command = new SqlCommand(sqlQuery, connection);

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

            using (SqlConnection connection = new SqlConnection(conn.ConnectionString))
            {
                await connection.OpenAsync();

                string sql = @"SELECT SCHEMA_NAME(schema_id) + '.' + o.Name AS 'TableName', 
                              c.Name as 'ColumnName',
                              CASE 
                                WHEN t.name IN ('char', 'varchar', 'nchar', 'nvarchar') THEN t.name + '(' + CAST(c.max_length as VARCHAR) + ')'
                                WHEN t.name IN ('decimal', 'numeric') THEN t.name + '(' + CAST(c.precision as VARCHAR) + ',' + CAST(c.scale as VARCHAR) + ')'
                                ELSE t.name
                              END as 'DataType'
                FROM     sys.columns c
                         JOIN sys.objects o ON o.object_id = c.object_id
                         JOIN sys.types t ON c.user_type_id = t.user_type_id
                WHERE    o.type = 'U'
                ORDER BY o.Name, c.column_id";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            rows.Add((reader.GetValue(0).ToString() ?? "", reader.GetValue(1).ToString() ?? "", reader.GetValue(2).ToString() ?? ""));
                        }
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
