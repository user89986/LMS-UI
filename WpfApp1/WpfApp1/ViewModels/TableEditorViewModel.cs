using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Dapper;

public class TableEditorViewModel : ViewModelBase
{
    private readonly DatabaseService _dbService;
    private readonly JsonExportService _exportService;
    private readonly string _tableName;


    private DataTable _tableData;
    public DataTable TableData
    {
        get => _tableData;
        set => SetProperty(ref _tableData, value);
    }

    public string Title => $"Editing: {_tableName}";
    private DataRowView _selectedRow;
    public DataRowView SelectedRow
    {
        get => _selectedRow;
        set => SetProperty(ref _selectedRow, value);
    }
    public ICommand ImportCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand DeleteRowCommand { get; }

    public TableEditorViewModel(string tableName)
    {
        _dbService = new DatabaseService();
        _exportService = new JsonExportService();
        _tableName = tableName;

        SaveCommand = new RelayCommand(async () => await SaveChanges());
        ExportCommand = new RelayCommand(async () => await ExportData());
        DeleteRowCommand = new RelayCommand(DeleteRow, CanDeleteRow);
        ImportCommand = new RelayCommand(async () => await ImportData());

        LoadTableData();
    }
    private bool CanDeleteRow()
    {
        return SelectedRow != null;
    }

    private async void DeleteRow()
    {
        if (SelectedRow == null) return;

        try
        {
            // Получаем список первичных ключей для таблицы
            using var connection = await _dbService.GetConnectionAsync();
            var primaryKeys = await _dbService.GetPrimaryKeyColumnsAsync(connection, _tableName);

            if (!primaryKeys.Any())
            {
                throw new Exception("Table has no primary key defined");
            }

            // Собираем значения первичных ключей
            var pkValues = new Dictionary<string, object>();
            foreach (var pkColumn in primaryKeys)
            {
                pkValues[pkColumn] = SelectedRow.Row[pkColumn];
            }

            // Удаляем из БД
            bool dbDeleted = await _dbService.DeleteRowByPkAsync(_tableName, pkValues);

            if (!dbDeleted)
            {
                throw new Exception("Failed to delete row from database");
            }

            // Удаляем из DataTable
            SelectedRow.Row.Delete();
            TableData.AcceptChanges();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting row: {ex.Message}");
            TableData.RejectChanges();
        }
    }

    private async void LoadTableData()
    {
        try
        {
            TableData = await _dbService.GetTableDataAsync(_tableName);
            TableData.TableName = _tableName; // Устанавливаем имя таблицы
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading table data: {ex.Message}");
        }
    }

    private async Task SaveChanges()
    {
        try
        {
            // Валидация данных перед сохранением
            foreach (DataRow row in TableData.Rows)
            {
                if (row.RowState == DataRowState.Added || row.RowState == DataRowState.Modified)
                {
                    if (_tableName == "users")
                    {
                        if (string.IsNullOrEmpty(row["login"]?.ToString()))
                            throw new Exception("Login cannot be empty");
                        if (string.IsNullOrEmpty(row["email"]?.ToString()))
                            throw new Exception("Email cannot be empty");
                    }
                }
            }

            await _dbService.UpdateTableAsync(TableData);
            MessageBox.Show("Changes saved successfully");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving changes: {ex.Message}\n\nDetails: {ex.InnerException?.Message}",
                          "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportData()
    {
        try
        {
            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Exports");
            var data = TableData.AsEnumerable().Select(row =>
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn column in TableData.Columns)
                {
                    dict[column.ColumnName] = row[column];
                }
                return dict;
            }).ToList();

            await _exportService.ExportToFileAsync(_tableName, data, directoryPath);
            MessageBox.Show($"Data exported successfully to Data/Exports folder");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export error: {ex.Message}");
        }
    }
    private async Task ImportData()
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Exports")
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var importService = new JsonImportService();
                var importedData = await importService.ImportFromFileAsync(openFileDialog.FileName);

                if (importedData == null || !importedData.Any())
                {
                    MessageBox.Show("No data to import or file is empty");
                    return;
                }

                // Проверяем совместимость структуры
                var firstItem = importedData.First();
                var missingColumns = new List<string>();

                foreach (DataColumn column in TableData.Columns)
                {
                    if (!firstItem.ContainsKey(column.ColumnName))
                    {
                        missingColumns.Add(column.ColumnName);
                    }
                }

                if (missingColumns.Any())
                {
                    var result = MessageBox.Show(
                        $"Imported data is missing columns: {string.Join(", ", missingColumns)}\n" +
                        "Continue import for matching columns only?",
                        "Data Mismatch",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes) return;
                }

                // Импортируем данные
                int importedCount = 0;
                foreach (var item in importedData)
                {
                    try
                    {
                        var newRow = TableData.NewRow();
                        bool hasValidData = false;

                        foreach (DataColumn column in TableData.Columns)
                        {
                            if (item.TryGetValue(column.ColumnName, out var value))
                            {
                                try
                                {
                                    newRow[column.ColumnName] = Convert.ChangeType(value, column.DataType);
                                    hasValidData = true;
                                }
                                catch
                                {
                                    // Если преобразование типа не удалось, используем значение по умолчанию
                                    newRow[column.ColumnName] = column.DefaultValue;
                                }
                            }
                        }

                        if (hasValidData)
                        {
                            TableData.Rows.Add(newRow);
                            importedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error importing row: {ex.Message}");
                    }
                }

                MessageBox.Show($"Successfully imported {importedCount} of {importedData.Count} records");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import error: {ex.Message}\n\nDetails: {ex.InnerException?.Message}",
                          "Import Error",
                          MessageBoxButton.OK,
                          MessageBoxImage.Error);
        }
    }
}
