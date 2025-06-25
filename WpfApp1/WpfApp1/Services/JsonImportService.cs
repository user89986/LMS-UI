using System.IO;
using System.Text.Json;
using WpfApp1.Models;

public class JsonImportService
{
    public async Task<List<Dictionary<string, object>>> ImportFromFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Import file not found", filePath);
            }

            var json = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // Десериализуем как динамический объект для гибкости
            var importData = JsonSerializer.Deserialize<JsonExportTemplate<JsonElement>>(json, options);

            if (importData?.Records == null)
            {
                throw new InvalidDataException("Invalid JSON format or empty data");
            }

            var result = new List<Dictionary<string, object>>();

            foreach (var record in importData.Records)
            {
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                // Обрабатываем каждое свойство в записи
                foreach (var prop in record.EnumerateObject())
                {
                    dict[prop.Name] = prop.Value switch
                    {
                        { ValueKind: JsonValueKind.String } => prop.Value.GetString(),
                        { ValueKind: JsonValueKind.Number } => prop.Value.GetDecimal(),
                        { ValueKind: JsonValueKind.True } => true,
                        { ValueKind: JsonValueKind.False } => false,
                        { ValueKind: JsonValueKind.Null } => null,
                        _ => prop.Value.ToString()
                    };
                }

                result.Add(dict);
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Import error: {ex.Message}");
            throw;
        }
    }
}