using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using TestKB.Models;

namespace TestKB.Services
{
    public class ContentService : IContentService
    {
        private readonly IWebHostEnvironment _env;
        private readonly JsonSerializerOptions _jsonOptions;

        // Added a lock object to handle concurrency on file read/write.
        private static readonly object _fileLock = new object();

        public ContentService(IWebHostEnvironment env)
        {
            _env = env;
            _jsonOptions = new JsonSerializerOptions
            {
                // This tells the serializer to use a less aggressive escaping strategy.
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };

        }

        public List<ContentItem> GetContentItems()
        {
            var jsonFilePath = Path.Combine(_env.WebRootPath, "data.json");

            lock (_fileLock)
            {
                if (!File.Exists(jsonFilePath))
                    return new List<ContentItem>();

                var jsonString = File.ReadAllText(jsonFilePath, Encoding.UTF8);
                try
                {
                    return JsonSerializer.Deserialize<List<ContentItem>>(jsonString, _jsonOptions)
                           ?? new List<ContentItem>();
                }
                catch (Exception ex)
                {
                    // Log the error and return empty if JSON is corrupted or unreadable.
                    Console.Error.WriteLine($"Error reading JSON file: {ex.Message}");
                    return new List<ContentItem>();
                }
            }
        }

        public void AddNewContent(string category, string subCategory, string content)
        {
            var items = GetContentItems();
            items.Add(new ContentItem
            {
                Category = category?.Trim(),
                SubCategory = subCategory?.Trim(),
                Content = content?.Trim()
            });
            SaveContentItems(items);
        }

        public void ExtendContent(string selectedCategory, string selectedSubCategory, string newSubCategory, string content)
        {
            var items = GetContentItems();
            var actualSubCategory = !string.IsNullOrWhiteSpace(newSubCategory)
                ? newSubCategory.Trim()
                : selectedSubCategory?.Trim();

            var existingEntry = items.FirstOrDefault(x =>
                x.Category.Equals(selectedCategory?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                x.SubCategory.Equals(actualSubCategory, StringComparison.OrdinalIgnoreCase));

            if (existingEntry != null)
            {
                existingEntry.Content = content?.Trim();
            }
            else
            {
                items.Add(new ContentItem
                {
                    Category = selectedCategory?.Trim(),
                    SubCategory = actualSubCategory,
                    Content = content?.Trim()
                });
            }

            SaveContentItems(items);
        }

        public void UpdateContentItems(List<ContentItem> items)
        {
            SaveContentItems(items);
        }

        private void SaveContentItems(List<ContentItem> items)
        {
            var jsonFilePath = Path.Combine(_env.WebRootPath, "data.json");

            lock (_fileLock)
            {
                var jsonString = JsonSerializer.Serialize(items, _jsonOptions);
                File.WriteAllText(jsonFilePath, jsonString, Encoding.UTF8);
            }
        }
    }
}
