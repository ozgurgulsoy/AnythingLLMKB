// Services/ContentService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TestKB.Models;
using TestKB.Repositories;
using TestKB.Services.Interfaces;

namespace TestKB.Services
{
    /// <summary>
    /// İçerik verilerine erişim için temel servis
    /// </summary>
    public class ContentService : IContentService
    {
        private readonly IContentRepository _repository;
        private readonly ILogger<ContentService> _logger;
        private readonly ICacheService _cacheService;
        
        // Cache key constants
        private const string CONTENT_ITEMS_CACHE_KEY = "ContentItems";
        
        public ContentService(
            IContentRepository repository,
            ILogger<ContentService> logger,
            ICacheService cacheService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }
        
        /// <summary>
        /// Tüm içerik öğelerini asenkron olarak getirir.
        /// </summary>
        public async Task<List<ContentItem>> GetAllAsync(bool forceReload = false)
        {
            if (forceReload)
            {
                _logger.LogInformation("İçerik öğeleri doğrudan repository'den alınıyor (önbellek atlanıyor)");
                _cacheService.Remove(CONTENT_ITEMS_CACHE_KEY);
                return await _repository.GetAllAsync();
            }

            return await _cacheService.GetOrSetAsync(CONTENT_ITEMS_CACHE_KEY, async () => 
            {
                _logger.LogInformation("İçerik öğeleri repository'den alınıyor");
                return await _repository.GetAllAsync();
            }, TimeSpan.FromMinutes(5));
        }
        
        /// <summary>
        /// Verilen kategori ve alt kategoriye göre içerik öğesini getirir
        /// </summary>
        public async Task<ContentItem> GetByCategoryAndSubcategoryAsync(string category, string subcategory)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Kategori boş olamaz", nameof(category));
                
            var items = await GetAllAsync();
            return items.FirstOrDefault(i => 
                string.Equals(i.Category.Trim(), category.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(i.SubCategory?.Trim(), subcategory?.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// Yeni bir içerik öğesi ekler
        /// </summary>
        public async Task<ContentItem> CreateAsync(ContentItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
                
            if (string.IsNullOrWhiteSpace(item.Category))
                throw new ArgumentException("Kategori boş olamaz", nameof(item.Category));
                
            var items = await GetAllAsync(true); // Always get fresh data
            
            // Check if item already exists
            var exists = items.Any(i => 
                string.Equals(i.Category.Trim(), item.Category.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(i.SubCategory?.Trim(), item.SubCategory?.Trim(), StringComparison.OrdinalIgnoreCase));
                
            if (exists)
                throw new InvalidOperationException($"İçerik zaten mevcut: {item.Category}/{item.SubCategory}");
                
            // Add the new item
            items.Add(item);
            await _repository.SaveAsync(items);
            
            // Invalidate cache
            InvalidateCache();
            
            _logger.LogInformation("Yeni içerik eklendi: {Category}/{SubCategory}", item.Category, item.SubCategory);
            
            return item;
        }
        
        /// <summary>
        /// Var olan içerik öğesini günceller
        /// </summary>
        public async Task<ContentItem> UpdateAsync(ContentItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
                
            if (string.IsNullOrWhiteSpace(item.Category))
                throw new ArgumentException("Kategori boş olamaz", nameof(item.Category));
                
            var items = await GetAllAsync(true); // Always get fresh data
            
            // Find the item to update
            var existingItem = items.FirstOrDefault(i => 
                string.Equals(i.Category.Trim(), item.Category.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(i.SubCategory?.Trim(), item.SubCategory?.Trim(), StringComparison.OrdinalIgnoreCase));
                
            if (existingItem == null)
                throw new InvalidOperationException($"İçerik bulunamadı: {item.Category}/{item.SubCategory}");
                
            // Update the item
            existingItem.Content = item.Content;
            
            await _repository.SaveAsync(items);
            
            // Invalidate cache
            InvalidateCache();
            
            _logger.LogInformation("İçerik güncellendi: {Category}/{SubCategory}", item.Category, item.SubCategory);
            
            return existingItem;
        }
        
        /// <summary>
        /// İçerik öğelerini toplu günceller
        /// </summary>
        public async Task UpdateManyAsync(List<ContentItem> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
                
            await _repository.SaveAsync(items);
            
            // Invalidate cache
            InvalidateCache();
            
            _logger.LogInformation("İçerik öğeleri toplu güncellendi, {Count} öğe", items.Count);
        }
        
        /// <summary>
        /// İçerik öğesini siler
        /// </summary>
        public async Task DeleteAsync(string category, string subcategory)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Kategori boş olamaz", nameof(category));
                
            var items = await GetAllAsync(true); // Always get fresh data
            
            // Find and remove the item
            var removed = items.RemoveAll(i => 
                string.Equals(i.Category.Trim(), category.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(i.SubCategory?.Trim(), subcategory?.Trim(), StringComparison.OrdinalIgnoreCase));
                
            if (removed == 0)
                throw new InvalidOperationException($"Silinecek içerik bulunamadı: {category}/{subcategory}");
                
            await _repository.SaveAsync(items);
            
            // Invalidate cache
            InvalidateCache();
            
            _logger.LogInformation("İçerik silindi: {Category}/{SubCategory}", category, subcategory);
        }
        
        /// <summary>
        /// Kategori ismine göre içerik öğelerini siler
        /// </summary>
        public async Task<int> DeleteByCategoryAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Kategori boş olamaz", nameof(category));
                
            var items = await GetAllAsync(true); // Always get fresh data
            
            // Remove all items with matching category
            var removedCount = items.RemoveAll(i => 
                string.Equals(i.Category.Trim(), category.Trim(), StringComparison.OrdinalIgnoreCase));
                
            if (removedCount == 0)
                return 0; // No items removed
                
            await _repository.SaveAsync(items);
            
            // Invalidate cache
            InvalidateCache();
            
            _logger.LogInformation("Kategori silindi: {Category}, {Count} öğe silindi", category, removedCount);
            
            return removedCount;
        }
        
        /// <summary>
        /// Önbelleği geçersiz kılar
        /// </summary>
        private void InvalidateCache()
        {
            _cacheService.Remove(CONTENT_ITEMS_CACHE_KEY);
            _cacheService.RemoveByPrefix("Content");
            _logger.LogDebug("İçerik önbellekleri temizlendi");
        }
    }
}