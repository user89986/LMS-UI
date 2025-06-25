using System.Data;
using System.Windows;

namespace WpfApp1
{
    public partial class TableEditorWindow : Window
    {
        public TableEditorWindow(DatabaseTable table)
        {
            InitializeComponent();
            DataContext = new TableEditorViewModel(table.Name);
        }

        public TableEditorWindow(DataTable tableData) : this(new DatabaseTable { Name = tableData.TableName })
        {
            TableData = tableData;
            ((TableEditorViewModel)DataContext).TableData = tableData;
        }

        public DataTable TableData { get; }
    }
}