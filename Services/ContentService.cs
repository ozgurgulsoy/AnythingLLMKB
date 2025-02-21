using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using TestKB.Models;

namespace TestKB.Services
{
    public class ContentService : IContentService
    {
        private readonly IWebHostEnvironment _env;
        private readonly JsonSerializerOptions _jsonOptions;
        private static readonly object _fileLock = new object();
        private const string DATA_FILE_NAME = "data.json";

        public ContentService(IWebHostEnvironment env)
        {
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };
        }

        private List<ContentItem> ReadContentItemsFromFile()
        {
            var jsonFilePath = GetJsonFilePath();
            lock (_fileLock)
            {
                if (!File.Exists(jsonFilePath))
                {
                    return new List<ContentItem>();
                }

                try
                {
                    var jsonString = File.ReadAllText(jsonFilePath, Encoding.UTF8);
                    return JsonSerializer.Deserialize<List<ContentItem>>(jsonString, _jsonOptions)
                           ?? new List<ContentItem>();
                }
                catch (Exception ex)
                {
                    LogError($"Error reading or deserializing JSON file: {ex.Message}");
                    return new List<ContentItem>();
                }
            }
        }

        public List<ContentItem> GetContentItems(bool forceReload = false)
        {
            return ReadContentItemsFromFile();
        }

        public void AddNewContent(string category, string subCategory, string content)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(subCategory) || string.IsNullOrWhiteSpace(content))
            {
                LogError("Category, SubCategory, or Content cannot be null or empty.");
                return;
            }

            var items = ReadContentItemsFromFile();
            items.Add(new ContentItem
            {
                Category = category.Trim(),
                SubCategory = subCategory.Trim(),
                Content = content.Trim()
            });

            SaveContentItems(items);
        }

        public void ExtendContent(string selectedCategory, string selectedSubCategory, string newSubCategory, string content)
        {
            if (string.IsNullOrWhiteSpace(selectedCategory) || string.IsNullOrWhiteSpace(content))
            {
                LogError("SelectedCategory or Content cannot be null or empty.");
                return;
            }

            var items = ReadContentItemsFromFile();
            var actualSubCategory = !string.IsNullOrWhiteSpace(newSubCategory)
                ? newSubCategory.Trim()
                : selectedSubCategory?.Trim();

            var existingEntry = items.FirstOrDefault(x =>
                x.Category.Equals(selectedCategory.Trim(), StringComparison.OrdinalIgnoreCase) &&
                x.SubCategory.Equals(actualSubCategory, StringComparison.OrdinalIgnoreCase));

            if (existingEntry != null)
            {
                existingEntry.Content = content.Trim();
            }
            else
            {
                items.Add(new ContentItem
                {
                    Category = selectedCategory.Trim(),
                    SubCategory = actualSubCategory,
                    Content = content.Trim()
                });
            }

            SaveContentItems(items);
        }

        public void UpdateContentItems(List<ContentItem> items)
        {
            if (items == null)
            {
                LogError("Items list cannot be null.");
                return;
            }

            SaveContentItems(items);
        }

        private void SaveContentItems(List<ContentItem> items)
        {
            var jsonFilePath = GetJsonFilePath();
            lock (_fileLock)
            {
                try
                {
                    var jsonString = JsonSerializer.Serialize(items, _jsonOptions);
                    File.WriteAllText(jsonFilePath, jsonString, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    LogError($"Error writing to JSON file: {ex.Message}");
                }
            }
        }

        private void LogError(string message)
        {
            Console.Error.WriteLine($"[ERROR] {DateTime.UtcNow}: {message}");
        }

        private string GetJsonFilePath()
        {
            return Path.Combine(_env.WebRootPath, DATA_FILE_NAME);
        }
    }
}
