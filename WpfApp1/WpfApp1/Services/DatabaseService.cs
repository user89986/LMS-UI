using Npgsql;
using Dapper;
using System.Configuration;
using System.Data;
public class DatabaseService
{
    private readonly string _connectionString;
    public DatabaseService()
    {
        _connectionString = ConfigurationManager.ConnectionStrings["PostgreSQL"].ConnectionString;
    }

    public async Task<NpgsqlConnection> GetConnectionAsync()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<DataTable> GetTableDataAsync(string tableName)
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

    public async Task UpdateTableAsync(DataTable table)
    {
        if (table == null) throw new ArgumentNullException(nameof(table));

        using var connection = await GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            var primaryKeyColumns = await GetPrimaryKeyColumnsAsync(connection, table.TableName);
            var columnsInfo = await GetTableColumnsInfoAsync(connection, table.TableName);

            foreach (DataRow row in table.Rows)
            {
                if (row.RowState == DataRowState.Added)
                {
                    foreach (var pkColumn in primaryKeyColumns)
                    {
                        if (row[pkColumn] == DBNull.Value || (row[pkColumn] is int && (int)row[pkColumn] == 0))
                        {
                            var sequenceQuery = $"SELECT nextval(pg_get_serial_sequence('{table.TableName}', '{pkColumn}'))";
                            var newId = await connection.ExecuteScalarAsync<int>(sequenceQuery, transaction: transaction);
                            row[pkColumn] = newId;
                        }
                    }
                }

                foreach (DataColumn column in table.Columns)
                {
                    if (row[column] == DBNull.Value && columnsInfo.TryGetValue(column.ColumnName, out var info) && !info.IsNullable)
                    {
                        row[column] = GetDefaultValueForType(info.DataType);
                    }
                }
            }
            var deletedRows = table.GetChanges(DataRowState.Deleted)?.Rows;
            if (deletedRows != null)
            {
                foreach (DataRow row in deletedRows)
                {
                    await DeleteRowAsync(connection, transaction, table.TableName, row, primaryKeyColumns);
                }
            }

            // Обработка измененных и добавленных строк
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
            table.AcceptChanges(); // Подтверждаем изменения после успешного сохранения
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            table.RejectChanges(); // Откатываем изменения в DataTable при ошибке
            throw new Exception($"Error updating table {table.TableName}: {ex.Message}", ex);
        }
    }
    public async Task<Dictionary<string, (string DataType, bool IsNullable)>> GetTableColumnsInfoAsync(NpgsqlConnection connection, string tableName)
    {
        var query = @"
        SELECT column_name, data_type, is_nullable 
        FROM information_schema.columns 
        WHERE table_name = @TableName";

        var columns = await connection.QueryAsync<(string column_name, string data_type, string is_nullable)>(
            query, new { TableName = tableName });

        return columns.ToDictionary(
            c => c.column_name,
            c => (c.data_type, c.is_nullable == "YES")
        );
    }

    private object GetDefaultValueForType(string dataType)
    {
        return dataType.ToLower() switch
        {
            "integer" or "bigint" => 0,
            "boolean" => false,
            "character varying" or "text" => string.Empty,
            "timestamp without time zone" => DateTime.Now,
            _ => null
        };
    }

   public async Task<List<string>> GetPrimaryKeyColumnsAsync(NpgsqlConnection connection, string tableName)
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

        foreach (DataColumn column in row.Table.Columns)
        {
            if (!primaryKeyColumns.Contains(column.ColumnName))
            {
                setClauses.Add($"{column.ColumnName} = @{column.ColumnName}");
                parameters.Add(column.ColumnName, row[column]);
            }
        }

        foreach (var pkColumn in primaryKeyColumns)
        {
            whereClauses.Add($"{pkColumn} = @pk_{pkColumn}");
            parameters.Add($"pk_{pkColumn}", row[pkColumn, DataRowVersion.Original]);
        }

        var query = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} " +
                    $"WHERE {string.Join(" AND ", whereClauses)}";

        await connection.ExecuteAsync(query, parameters, transaction);
    }
    private async Task DeleteRowAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
                                string tableName, DataRow row, List<string> primaryKeyColumns)
    {
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();
        DataRow originalRow = null;

        if (row.RowState == DataRowState.Deleted)
        {
            foreach (var pkColumn in primaryKeyColumns)
            {
                if (row.Table.Columns.Contains(pkColumn))
                {
                    var originalValue = row[pkColumn, DataRowVersion.Original];
                    whereClauses.Add($"{pkColumn} = @{pkColumn}");
                    parameters.Add(pkColumn, originalValue);
                }
            }
        }
        else 
        {
            foreach (var pkColumn in primaryKeyColumns)
            {
                whereClauses.Add($"{pkColumn} = @{pkColumn}");
                parameters.Add(pkColumn, row[pkColumn]);
            }
        }
        if (!whereClauses.Any())
        {
            throw new InvalidOperationException("No valid primary key columns found for deletion");
        }

        var query = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereClauses)}";
        await connection.ExecuteAsync(query, parameters, transaction);
    }
    public async Task<bool> DeleteRowByPkAsync(string tableName, Dictionary<string, object> pkValues)
    {
        using var connection = await GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync();
            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            foreach (var kvp in pkValues)
            {
                whereClauses.Add($"{kvp.Key} = @{kvp.Key}");
                parameters.Add(kvp.Key, kvp.Value);
            }
            var checkQuery = $"SELECT 1 FROM {tableName} WHERE {string.Join(" AND ", whereClauses)} LIMIT 1";
            var exists = await connection.ExecuteScalarAsync<int?>(checkQuery, parameters, transaction);

            if (!exists.HasValue) return false;
            var deleteQuery = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereClauses)}";
            int affectedRows = await connection.ExecuteAsync(deleteQuery, parameters, transaction);

            await transaction.CommitAsync();
            return affectedRows > 0;    
    }
    public async Task<List<string>> GetTableColumnsAsync(NpgsqlConnection connection, string tableName)
    {
        var query = @"
    SELECT column_name
    FROM information_schema.columns
    WHERE table_name = @TableName
    ORDER BY ordinal_position";

        var columns = await connection.QueryAsync<string>(query, new { TableName = tableName });
        return columns.ToList();
    }
    public async Task<int> ImportDataAsync(string tableName, IEnumerable<Dictionary<string, object>> records)
    {
        using var connection = await GetConnectionAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            int importedCount = 0;
            var columns = await GetTableColumnsAsync(connection, tableName);
            var primaryKeys = await GetPrimaryKeyColumnsAsync(connection, tableName);

            foreach (var record in records)
            {
                // Build the INSERT statement
                var columnsToInsert = record.Keys.Where(k => columns.Contains(k));
                var columnNames = string.Join(", ", columnsToInsert);
                var valuePlaceholders = string.Join(", ", columnsToInsert.Select(c => $"@{c}"));

                var sql = $"INSERT INTO {tableName} ({columnNames}) VALUES ({valuePlaceholders})";

                // Execute the insert
                importedCount += await connection.ExecuteAsync(sql, record, transaction);
            }

            transaction.Commit();
            return importedCount;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

}
    


