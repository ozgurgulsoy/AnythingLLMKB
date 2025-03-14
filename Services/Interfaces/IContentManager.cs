// Services/Interfaces/IContentManager.cs
using TestKB.Models;
using TestKB.Models.ViewModels;

namespace TestKB.Services.Interfaces
{
    /// <summary>
    /// İş mantığı ve görünüm modeli dönüşümlerinden sorumlu arayüz
    /// </summary>
    public interface IContentManager
    {
        /// <summary>
        /// İçerik listesi görünüm modelini oluşturur
        /// </summary>
        Task<ContentListViewModel> BuildContentListViewModelAsync(string category, Department department);
        
        /// <summary>
        /// Düzenleme sayfası görünüm modelini oluşturur
        /// </summary>
        Task<EditContentViewModel> BuildEditContentViewModelAsync(NewContentViewModel newContent, ExtendContentViewModel extendContent, List<ContentItem> preloadedItems = null);
        
        /// <summary>
        /// Var olan içeriği düzenlemek için görünüm modeli oluşturur
        /// </summary>
        Task<ExtendContentViewModel> CreateExtendContentViewModelAsync(string selectedCategory, string selectedSubCategory, Department department);
        
        /// <summary>
        /// Yeni içerik ekler
        /// </summary>
        Task AddNewContentAsync(NewContentViewModel model);
        
        /// <summary>
        /// Var olan içeriği günceller
        /// </summary>
        Task UpdateContentAsync(ExtendContentViewModel model);
        
        /// <summary>
        /// Kategori adını günceller
        /// </summary>
        Task UpdateCategoryAsync(string oldCategory, string newCategory);
        
        /// <summary>
        /// Alt kategori adını günceller
        /// </summary>
        Task UpdateSubCategoryAsync(string category, string oldSubCategory, string newSubCategory);
        
        /// <summary>
        /// Yeni alt kategori ekler
        /// </summary>
        Task AddSubCategoryAsync(string category, string newSubCategory);
        
        /// <summary>
        /// Kategoriyi ve ilgili tüm içerikleri siler
        /// </summary>
        Task<int> DeleteCategoryAsync(string category);
        
        /// <summary>
        /// Tüm kategori isimlerini getirir
        /// </summary>
        Task<List<string>> GetAllCategoriesAsync();
        
        /// <summary>
        /// Belirli bir kategoriye ait alt kategorileri getirir
        /// </summary>
        Task<List<string>> GetSubcategoriesAsync(string category);
    }
}