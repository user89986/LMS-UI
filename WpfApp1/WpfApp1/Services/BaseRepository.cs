using Npgsql;
using Dapper;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public abstract class BaseRepository
{
    private readonly string _connectionString;

    protected BaseRepository()
    {
        _connectionString = ConfigurationManager.ConnectionStrings["PostgreSQL"].ConnectionString;
    }

    protected async Task<NpgsqlConnection> GetConnectionAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    protected async Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters = null)
    {
        using var connection = await GetConnectionAsync();
        return await connection.QueryAsync<T>(sql, parameters);
    }

    protected async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object parameters = null)
    {
        using var connection = await GetConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters);
    }

    protected async Task<int> ExecuteAsync(string sql, object parameters = null)
    {
        using var connection = await GetConnectionAsync();
        return await connection.ExecuteAsync(sql, parameters);
    }

    protected async Task<T> ExecuteScalarAsync<T>(string sql, object parameters = null)
    {
        using var connection = await GetConnectionAsync();
        return await connection.ExecuteScalarAsync<T>(sql, parameters);
    }

    protected async Task<DataTable> GetTableDataAsync(string tableName)
    {
        using var connection = await GetConnectionAsync();
        try
        {
            var command = new NpgsqlCommand($"SELECT * FROM {tableName}", connection);
            var adapter = new NpgsqlDataAdapter(command);
            var dataTable = new DataTable();
            adapter.Fill(dataTable);
            return dataTable;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database error: {ex.Message}");
            return new DataTable();
        }
    }
}