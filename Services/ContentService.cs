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
    // İçerik verilerini yönetmek için servis sınıfı.
    public class ContentService(IWebHostEnvironment env) : IContentService
    {
        private readonly IWebHostEnvironment _env = env ?? throw new ArgumentNullException(nameof(env));
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        };
        private static readonly object FileLock = new object();
        private const string DataFileName = "data.json";

        // JSON dosyasından içerik okur.
        private List<ContentItem> ReadContentItemsFromFile()
        {
            var jsonFilePath = GetJsonFilePath();
            lock (FileLock)
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
                    LogError($"JSON dosyası okunurken veya deserialize edilirken hata: {ex.Message}");
                    return new List<ContentItem>();
                }
            }
        }

        // Dosyadan içerikleri alır. forceReload bu örnekte yok sayılır.
        public List<ContentItem> GetContentItems(bool forceReload = false)
        {
            return ReadContentItemsFromFile();
        }

        // Yeni içerik ekler.
        public void AddNewContent(string category, string subCategory, string content)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(subCategory) || string.IsNullOrWhiteSpace(content))
            {
                LogError("Kategori, Alt Kategori veya İçerik boş bırakılamaz.");
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

        // İçeriği genişletir veya ekler.
        public void ExtendContent(string selectedCategory, string selectedSubCategory, string newSubCategory, string content)
        {
            if (string.IsNullOrWhiteSpace(selectedCategory) || string.IsNullOrWhiteSpace(content))
            {
                LogError("Seçili Kategori veya İçerik boş bırakılamaz.");
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

        // Dışarıdan alınan içerik listesini dosyaya kaydeder.
        public void UpdateContentItems(List<ContentItem> items)
        {
            if (items == null)
            {
                LogError("İçerik listesi null olamaz.");
                return;
            }

            SaveContentItems(items);
        }

        // Listeyi JSON olarak kaydeder.
        private void SaveContentItems(List<ContentItem> items)
        {
            var jsonFilePath = GetJsonFilePath();
            lock (FileLock)
            {
                try
                {
                    var jsonString = JsonSerializer.Serialize(items, _jsonOptions);
                    File.WriteAllText(jsonFilePath, jsonString, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    LogError($"JSON dosyasına yazılırken hata: {ex.Message}");
                }
            }
        }

        // Hata mesajını konsola yazar.
        private void LogError(string message)
        {
            Console.Error.WriteLine($"[HATA] {DateTime.UtcNow}: {message}");
        }

        // JSON dosyasının tam yolunu verir.
        private string GetJsonFilePath()
        {
            return Path.Combine(_env.WebRootPath, DataFileName);
        }
    }
}
