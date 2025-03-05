using TestKB.Models;

namespace TestKB.Services.Interfaces
{
    /// <summary>
    /// Veri erişim katmanı - içerik verilerini depolama ve getirme sorumluluğu
    /// </summary>
    public interface IContentService
    {
        /// <summary>
        /// Tüm içerik öğelerini getirir
        /// </summary>
        Task<List<ContentItem>> GetAllAsync(bool forceReload = false);
        
        /// <summary>
        /// Verilen kategori ve alt kategoriye göre içerik öğesini getirir
        /// </summary>
        Task<ContentItem> GetByCategoryAndSubcategoryAsync(string category, string subcategory);
        
        /// <summary>
        /// Verilen kategori, alt kategori ve departmana göre içerik öğesini getirir
        /// </summary>
        Task<ContentItem> GetByCategoryAndSubcategoryAsync(string category, string subcategory, Department department);
        
        /// <summary>
        /// Yeni bir içerik öğesi ekler
        /// </summary>
        Task<ContentItem> CreateAsync(ContentItem item);
        
        /// <summary>
        /// Var olan içerik öğesini günceller
        /// </summary>
        Task<ContentItem> UpdateAsync(ContentItem item);
        
        /// <summary>
        /// İçerik öğelerini toplu günceller
        /// </summary>
        Task UpdateManyAsync(List<ContentItem> items);
        
        /// <summary>
        /// İçerik öğesini siler
        /// </summary>
        Task DeleteAsync(string category, string subcategory);
        
        /// <summary>
        /// Kategori ismine göre içerik öğelerini siler
        /// </summary>
        Task<int> DeleteByCategoryAsync(string category);
    }
}