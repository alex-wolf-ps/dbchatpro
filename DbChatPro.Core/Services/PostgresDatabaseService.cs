using DBChatPro.Models;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace DBChatPro
{
    public class PostgresDatabaseService : IDatabaseService
    {
        public async Task<List<List<string>>> GetDataTable(AIConnection conn, string sqlQuery)
        {
            var rows = new List<List<string>>();
            var dataSource = NpgsqlDataSource.Create(conn.ConnectionString);

            await using (var cmd = dataSource.CreateCommand(sqlQuery))
            
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
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
            }

            return rows;
        }

        public async Task<DatabaseSchema> GenerateSchema(AIConnection conn)
        {
            var dbSchema = new DatabaseSchema() { SchemaRaw = new List<string>(), SchemaStructured = new List<TableSchema>() };
            List<(string TableName, string ColumnName, string DataType)> rows = new();

            var pairs = conn.ConnectionString.Split(";");
            var database = pairs.Where(x => x.Contains("Database")).FirstOrDefault().Split("=").Last();
            var schemaName = pairs.Where(x => x.Contains("SearchPath")).DefaultIfEmpty("public").FirstOrDefault().Split("=").Last();

            string sqlQuery = $@"SELECT 
                                    table_name, 
                                    column_name,
                                    CASE 
                                        WHEN character_maximum_length IS NOT NULL THEN data_type || '(' || character_maximum_length::text || ')'
                                        WHEN numeric_precision IS NOT NULL AND numeric_scale IS NOT NULL THEN data_type || '(' || numeric_precision::text || ',' || numeric_scale::text || ')'
                                        ELSE data_type
                                    END as data_type_formatted
                                FROM 
                                    information_schema.columns 
                                WHERE 
                                    table_catalog = $1
                                    AND table_schema = $2
                                ORDER BY 
                                    table_name, 
                                    ordinal_position;";


            var dataSourceBuilder = new NpgsqlDataSourceBuilder(conn.ConnectionString);
            var dataSource = dataSourceBuilder.Build();

            var connection = await dataSource.OpenConnectionAsync();

            await using (var cmd = new NpgsqlCommand(sqlQuery, connection)
            {
                Parameters =
                {
                    new() { Value = database },
                    new() { Value = schemaName }
                }
            })

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    rows.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
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
