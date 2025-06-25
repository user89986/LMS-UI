using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Windows;
using System.Data;
using WpfApp1;

public class MainViewModel : ViewModelBase
{
    private readonly IDataService _dataService;
    private readonly IUserService _userService;

    private ObservableCollection<DatabaseTable> _tables = new ObservableCollection<DatabaseTable>();
    public ObservableCollection<DatabaseTable> Tables
    {
        get => _tables;
        set => SetProperty(ref _tables, value);
    }

    private DatabaseTable _selectedTable;
    public DatabaseTable SelectedTable
    {
        get => _selectedTable;
        set
        {
            if (SetProperty(ref _selectedTable, value))
            {
                OpenTableEditor(value);
            }
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand LoadTablesCommand { get; }
    public ICommand RefreshCommand { get; }

    public MainViewModel(IDataService dataService, IUserService userService)
    {
        _dataService = dataService;
        _userService = userService;

        LoadTablesCommand = new RelayCommand(async () => await LoadTablesAsync());
        RefreshCommand = new RelayCommand(async () => await LoadTablesAsync());

        // Загружаем таблицы при инициализации
        LoadTablesCommand.Execute(null);
    }

    private async Task LoadTablesAsync()
    {
        try
        {
            IsLoading = true;
            var tables = await _dataService.GetDatabaseTablesAsync();

            Tables.Clear();
            foreach (var table in tables.OrderBy(t => t.Name))
            {
                Tables.Add(table);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading tables: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void OpenTableEditor(DatabaseTable table)
    {
        if (table == null) return;

        try
        {
            IsLoading = true;
            var tableData = await _dataService.GetTableDataAsync(table.Name);
            tableData.TableName = table.Name;

            var editorWindow = new TableEditorWindow(tableData);
            editorWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening table editor: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}