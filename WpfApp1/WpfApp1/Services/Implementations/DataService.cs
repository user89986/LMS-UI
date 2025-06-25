using System.Data;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Npgsql;

public class DataService : BaseRepository, IDataService
{
    public async Task<IEnumerable<DatabaseTable>> GetDatabaseTablesAsync()
    {
        try
        {
            var query = @"
                SELECT table_name as Name, 
                       (SELECT count(*) FROM information_schema.tables t WHERE t.table_name = tables.table_name) as RowCount
                FROM information_schema.tables
                WHERE table_schema = 'public'";

            return await QueryAsync<DatabaseTable>(query);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database error: {ex.Message}");
            return new List<DatabaseTable>();
        }
    }

    public async Task<DataTable> GetTableDataAsync(string tableName)
    {
        try
        {
            using var connection = await GetConnectionAsync();
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

    public async Task UpdateTableAsync(DataTable table)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));

        using var connection = await GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var primaryKeyColumns = await GetPrimaryKeyColumnsAsync(connection, table.TableName);

            if (!primaryKeyColumns.Any())
            {
                throw new InvalidOperationException($"Table {table.TableName} has no primary key defined");
            }

            // Process deleted rows first
            var deletedRows = table.GetChanges(DataRowState.Deleted)?.Rows;
            if (deletedRows != null)
            {
                foreach (DataRow row in deletedRows)
                {
                    await DeleteRowAsync(connection, transaction, table.TableName, row, primaryKeyColumns);
                }
            }

            // Then process added and modified rows
            foreach (DataRow row in table.Rows)
            {
                if (row.RowState == DataRowState.Added)
                {
                    await InsertRowAsync(connection, transaction, table.TableName, row);
                }
                else if (row.RowState == DataRowState.Modified)
                {
                    await UpdateRowAsync(connection, transaction, table.TableName, row, primaryKeyColumns);
                }
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception($"Error updating table {table.TableName}: {ex.Message}", ex);
        }
    }

    private async Task<List<string>> GetPrimaryKeyColumnsAsync(NpgsqlConnection connection, string tableName)
    {
        var query = @"
        SELECT a.attname
        FROM pg_index i
        JOIN pg_attribute a ON a.attrelid = i.indrelid AND a.attnum = ANY(i.indkey)
        WHERE i.indrelid = @TableName::regclass
        AND i.indisprimary";

        var primaryKeys = await connection.QueryAsync<string>(query, new { TableName = tableName });
        return primaryKeys.ToList();
    }

    private async Task InsertRowAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
                                    string tableName, DataRow row)
    {
        var columns = new List<string>();
        var parameters = new DynamicParameters();

        foreach (DataColumn column in row.Table.Columns)
        {
            columns.Add(column.ColumnName);
            parameters.Add(column.ColumnName, row[column]);
        }

        var columnsList = string.Join(", ", columns);
        var valuesList = string.Join(", ", columns.Select(c => $"@{c}"));

        var query = $"INSERT INTO {tableName} ({columnsList}) VALUES ({valuesList})";

        await connection.ExecuteAsync(query, parameters, transaction);
    }

    private async Task UpdateRowAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
                                    string tableName, DataRow row, List<string> primaryKeyColumns)
    {
        var setClauses = new List<string>();
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        // Form SET part
        foreach (DataColumn column in row.Table.Columns)
        {
            if (!primaryKeyColumns.Contains(column.ColumnName))
            {
                setClauses.Add($"{column.ColumnName} = @{column.ColumnName}");
                parameters.Add(column.ColumnName, row[column]);
            }
        }

        // Form WHERE part by primary key
        foreach (var pkColumn in primaryKeyColumns)
        {
            whereClauses.Add($"{pkColumn} = @pk_{pkColumn}");
            parameters.Add($"pk_{pkColumn}", row[pkColumn, DataRowVersion.Original]);
        }

        var query = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} " +
                    $"WHERE {string.Join(" AND ", whereClauses)}";

        await connection.ExecuteAsync(query, parameters, transaction);
    }

    public async Task DeleteRowAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
                                    string tableName, DataRow row, List<string> primaryKeyColumns)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        // For deleted rows use Original version of values
        foreach (var pkColumn in primaryKeyColumns)
        {
            whereClauses.Add($"{pkColumn} = @{pkColumn}");
            parameters.Add(pkColumn, row[pkColumn, DataRowVersion.Original]);
        }

        var query = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereClauses)}";

        await connection.ExecuteAsync(query, parameters, transaction);
    }
}