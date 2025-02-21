using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Retrieves all content items from the JSON file.
        /// </summary>
        /// <returns>A list of content items or an empty list if the file doesn't exist or is corrupted.</returns>
        public List<ContentItem> GetContentItems()
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

        /// <summary>
        /// Adds a new content item to the JSON file.
        /// </summary>
        /// <param name="category">The category of the content.</param>
        /// <param name="subCategory">The subcategory of the content.</param>
        /// <param name="content">The content text.</param>
        public void AddNewContent(string category, string subCategory, string content)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(subCategory) || string.IsNullOrWhiteSpace(content))
            {
                LogError("Category, SubCategory, or Content cannot be null or empty.");
                return;
            }

            var items = GetContentItems();
            items.Add(new ContentItem
            {
                Category = category.Trim(),
                SubCategory = subCategory.Trim(),
                Content = content.Trim()
            });

            SaveContentItems(items);
        }

        /// <summary>
        /// Extends or updates an existing content item in the JSON file.
        /// </summary>
        /// <param name="selectedCategory">The selected category.</param>
        /// <param name="selectedSubCategory">The selected subcategory.</param>
        /// <param name="newSubCategory">The new subcategory (optional).</param>
        /// <param name="content">The updated content text.</param>
        public void ExtendContent(string selectedCategory, string selectedSubCategory, string newSubCategory, string content)
        {
            if (string.IsNullOrWhiteSpace(selectedCategory) || string.IsNullOrWhiteSpace(content))
            {
                LogError("SelectedCategory or Content cannot be null or empty.");
                return;
            }

            var items = GetContentItems();
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

        /// <summary>
        /// Updates the entire list of content items in the JSON file.
        /// </summary>
        /// <param name="items">The updated list of content items.</param>
        public void UpdateContentItems(List<ContentItem> items)
        {
            if (items == null)
            {
                LogError("Items list cannot be null.");
                return;
            }

            SaveContentItems(items);
        }

        /// <summary>
        /// Saves the content items to the JSON file.
        /// </summary>
        /// <param name="items">The list of content items to save.</param>
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

        /// <summary>
        /// Logs errors to the console.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        private void LogError(string message)
        {
            Console.Error.WriteLine($"[ERROR] {DateTime.UtcNow}: {message}");
        }

        /// <summary>
        /// Gets the full path to the JSON data file.
        /// </summary>
        /// <returns>The full path to the JSON file.</returns>
        private string GetJsonFilePath()
        {
            return Path.Combine(_env.WebRootPath, DATA_FILE_NAME);
        }
    }
}