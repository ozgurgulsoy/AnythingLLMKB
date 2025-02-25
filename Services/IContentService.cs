// Services/IContentService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using TestKB.Models;

namespace TestKB.Services
{
    /// <summary>
    /// İçerik servisi için arayüz tanımı.
    /// </summary>
    public interface IContentService
    {
        /// <summary>
        /// Tüm içerik öğelerini asenkron olarak getirir.
        /// </summary>
        Task<List<ContentItem>> GetContentItemsAsync(bool forceReload = false);

        /// <summary>
        /// Yeni içerik asenkron olarak ekler.
        /// </summary>
        Task AddNewContentAsync(string category, string subCategory, string content);

        /// <summary>
        /// İçeriği asenkron olarak genişletir veya ekler.
        /// </summary>
        Task ExtendContentAsync(string selectedCategory, string selectedSubCategory, string newSubCategory, string content);

        /// <summary>
        /// İçerik öğelerini asenkron olarak günceller.
        /// </summary>
        Task UpdateContentItemsAsync(List<ContentItem> items);
    }
}