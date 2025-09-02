using DBChatPro.Models;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Types;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace DBChatPro
{
    public class DatabaseManagerService(
        MySqlDatabaseService mySqlDb, 
        SqlServerDatabaseService msSqlDb, 
        PostgresDatabaseService postgresDb, 
        OracleDatabaseService oracleDb,
        SnowflakeDatabaseService snowflakeDb) : IDatabaseService
    {
        public async Task<List<List<string>>> GetDataTable(AIConnection conn, string sqlQuery)
        {
            switch (conn.DatabaseType)
            {
                case "MSSQL":
                    return await msSqlDb.GetDataTable(conn, sqlQuery);
                case "MYSQL":
                    return await mySqlDb.GetDataTable(conn, sqlQuery);
                case "POSTGRESQL":
                    return await postgresDb.GetDataTable(conn, sqlQuery);
                case "ORACLE":
                    return await oracleDb.GetDataTable(conn, sqlQuery);
                case "SNOWFLAKE":
                    return await snowflakeDb.GetDataTable(conn, sqlQuery);
            }

            return null;
        }

        public async Task<DatabaseSchema> GenerateSchema(AIConnection conn)
        {
            switch (conn.DatabaseType)
            {
                case "MSSQL":
                    return await msSqlDb.GenerateSchema(conn);
                case "MYSQL":
                    return await mySqlDb.GenerateSchema(conn);
                case "POSTGRESQL":
                    return await postgresDb.GenerateSchema(conn);
                case "ORACLE":
                    return await oracleDb.GenerateSchema(conn);
                case "SNOWFLAKE":
                    return await snowflakeDb.GenerateSchema(conn);
            }

            return new() { SchemaStructured = new List<TableSchema>(), SchemaRaw = new List<string>() };
        }
    }
}
