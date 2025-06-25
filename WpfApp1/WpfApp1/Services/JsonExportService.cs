using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using WpfApp1.Models;


    public class JsonExportService
    {
        public async Task ExportToFileAsync<T>(string tableName, List<T> data, string directoryPath)
        {
            try
            {
                // Создаем шаблон для экспорта
                var exportData = new JsonExportTemplate<T>
                {
                    TableName = tableName,
                    RecordCount = data.Count,
                    Records = data
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // Формируем имя файла
                var fileName = $"{tableName.ToLower()}_export_{DateTime.Now:yyyyMMddHHmmss}.json";
                var fullPath = Path.Combine(directoryPath, fileName);

                // Создаем директорию, если ее нет
                Directory.CreateDirectory(directoryPath);

                // Сериализуем и сохраняем
                var json = JsonSerializer.Serialize(exportData, options);
                await File.WriteAllTextAsync(fullPath, json);

                Console.WriteLine($"Data exported successfully to {fullPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export error: {ex.Message}");
                throw;
            }
        }
    }
