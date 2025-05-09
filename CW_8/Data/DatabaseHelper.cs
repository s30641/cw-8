using Microsoft.Data.SqlClient;
using System.Data;

namespace CW_8.Data;

public static class DatabaseHelper
{
    private static readonly string _connectionString = "YourConnectionStringHere";

    public static SqlConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public static async Task<List<T>> ExecuteQueryAsync<T>(string query, Func<SqlDataReader, T> mapFunction)
    {
        var results = new List<T>();

        using (var connection = GetConnection())
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(query, connection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(mapFunction(reader));
                    }
                }
            }
        }

        return results;
    }

    public static async Task<int> ExecuteNonQueryAsync(string query, params SqlParameter[] parameters)
    {
        using (var connection = GetConnection())
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddRange(parameters);
                return await command.ExecuteNonQueryAsync();
            }
        }
    }
}