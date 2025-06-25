namespace WpfApp1.Models
{
    public class JsonExportTemplate<T>
    {
        public string TableName { get; set; }
        public int RecordCount { get; set; }
        public List<T> Records { get; set; }
        public string ExportDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    }
}