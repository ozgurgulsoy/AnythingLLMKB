// Services/ContentService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TestKB.Models;
using TestKB.Repositories;

namespace TestKB.Services
{
    /// <summary>
    /// İçerik servis implementasyonu. Repository kullanarak verilere erişir.
    /// </summary>
    public class ContentService : IContentService
    {
        private readonly IContentRepository _repository;
        private readonly ILogger<ContentService> _logger;

        public ContentService(
            IContentRepository repository,
            ILogger<ContentService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Tüm içerik öğelerini asenkron olarak getirir.
        /// </summary>
        public async Task<List<ContentItem>> GetContentItemsAsync(bool forceReload = false)
        {
            return await _repository.GetAllAsync();
        }

        /// <summary>
        /// Yeni içerik ekler.
        /// </summary>
        public async Task AddNewContentAsync(string category, string subCategory, string content)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(subCategory) || string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Kategori, Alt Kategori veya İçerik boş bırakılamaz.");
                throw new ArgumentException("Kategori, Alt Kategori veya İçerik boş bırakılamaz.");
            }

            var items = await _repository.GetAllAsync();
            items.Add(new ContentItem
            {
                Category = category.Trim(),
                SubCategory = subCategory.Trim(),
                Content = content.Trim()
            });

            await _repository.SaveAsync(items);
        }

        /// <summary>
        /// İçeriği genişletir veya ekler.
        /// </summary>
        public async Task ExtendContentAsync(string selectedCategory, string selectedSubCategory, string newSubCategory, string content)
        {
            if (string.IsNullOrWhiteSpace(selectedCategory) || string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Seçili Kategori veya İçerik boş bırakılamaz.");
                throw new ArgumentException("Seçili Kategori veya İçerik boş bırakılamaz.");
            }

            var items = await _repository.GetAllAsync();
            var actualSubCategory = !string.IsNullOrWhiteSpace(newSubCategory)
                ? newSubCategory.Trim()
                : selectedSubCategory?.Trim();

            var existingEntry = items.Find(x =>
                string.Equals(x.Category.Trim(), selectedCategory.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.SubCategory, actualSubCategory, StringComparison.OrdinalIgnoreCase));

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

            await _repository.SaveAsync(items);
        }

        /// <summary>
        /// İçerik öğelerini günceller.
        /// </summary>
        public async Task UpdateContentItemsAsync(List<ContentItem> items)
        {
            if (items == null)
            {
                _logger.LogWarning("İçerik listesi null olamaz.");
                throw new ArgumentNullException(nameof(items), "İçerik listesi null olamaz.");
            }

            await _repository.SaveAsync(items);
        }
    }
}