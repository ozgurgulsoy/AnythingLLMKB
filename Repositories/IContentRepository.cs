// Interfaces/IContentRepository.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using TestKB.Models;

namespace TestKB.Repositories
{
    /// <summary>
    /// İçerik verilerinin depolanması ve alınması için repository arayüzü.
    /// </summary>
    public interface IContentRepository
    {
        /// <summary>
        /// Tüm içerik öğelerini asenkron olarak getirir.
        /// </summary>
        Task<List<ContentItem>> GetAllAsync();

        /// <summary>
        /// İçerik öğelerini asenkron olarak kaydeder.
        /// </summary>
        Task SaveAsync(List<ContentItem> items);

        /// <summary>
        /// Depo dosyasının var olup olmadığını kontrol eder.
        /// </summary>
        Task<bool> ExistsAsync();
    }
}