// Services/ContentManager.cs
using TestKB.Models;
using TestKB.Models.ViewModels;
using TestKB.Services.Interfaces;

namespace TestKB.Services
{
    /// <summary>
    /// İçerik iş mantığı ve görünüm modeli dönüşümlerinden sorumlu servis
    /// </summary>
    public class ContentManager : IContentManager
    {
        private readonly IContentService _contentService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<ContentManager> _logger;

        // Önbellek anahtarı önekleri
        private const string CATEGORIES_CACHE_KEY = "Categories";
        private const string SUBCATEGORIES_CACHE_PREFIX = "Subcategories_";
        
        public ContentManager(
            IContentService contentService,
            ICacheService cacheService,
            ILogger<ContentManager> logger)
        {
            _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// İçerik listesi görünüm modelini oluşturur
        /// </summary>
        public async Task<ContentListViewModel> BuildContentListViewModelAsync(string category, Department department)
        {
            // Get all content items
            var items = await _contentService.GetAllAsync();
            
            // Get categories (can be cached)
            var categories = await GetAllCategoriesAsync();
            
            // Filter items by category and department
            var filteredItems = items.Where(i => 
                (string.IsNullOrWhiteSpace(category) || i.Category.Equals(category, StringComparison.OrdinalIgnoreCase)) && 
                i.Department == department
            ).ToList();
            
            return new ContentListViewModel
            {
                ContentItems = filteredItems,
                AllCategories = categories,
                SelectedCategory = category,
                SelectedDepartment = department
            };
        }
        
        /// <summary>
        /// Düzenleme sayfası görünüm modelini oluşturur
        /// </summary>
        public async Task<EditContentViewModel> BuildEditContentViewModelAsync(
            NewContentViewModel newContent, ExtendContentViewModel extendContent, List<ContentItem> preloadedItems = null)
        {
            // Always get fresh data for edit operations
            var items = preloadedItems?? await _contentService.GetAllAsync(true);
            
            // Get categories and subcategories
            var categories = GetDistinctOrderedCategories(items);
            var subCategories = BuildSubcategoryDictionary(items);
            
            // Set department in view model
            Department department = Department.Yazılım; // Default
            
            if (newContent != null && newContent.Department != 0)
            {
                department = newContent.Department;
            }
            else if (extendContent != null && extendContent.Department != 0)
            {
                department = extendContent.Department;
            }
            
            return new EditContentViewModel
            {
                ExistingCategories = categories,
                ExistingSubCategories = subCategories,
                NewContent = newContent ?? new NewContentViewModel(),
                ExtendContent = extendContent ?? new ExtendContentViewModel(),
                ContentItems = items,
                SelectedDepartment = department
            };
        }
        
        /// <summary>
        /// Var olan içeriği düzenlemek için görünüm modeli oluşturur
        /// </summary>
        public async Task<ExtendContentViewModel> CreateExtendContentViewModelAsync(
            string selectedCategory, string selectedSubCategory, Department department)
        {
            _logger.LogInformation("ExtendContentViewModel oluşturuluyor. Category: {Category}, SubCategory: {SubCategory}, Department: {Department}", 
                selectedCategory, selectedSubCategory, department);
                
            var model = new ExtendContentViewModel
            {
                SelectedCategory = selectedCategory,
                SelectedSubCategory = selectedSubCategory,
                Department = department
            };
            
            if (!string.IsNullOrWhiteSpace(selectedCategory) && !string.IsNullOrWhiteSpace(selectedSubCategory))
            {
                var item = await _contentService.GetByCategoryAndSubcategoryAsync(selectedCategory, selectedSubCategory, department);
                
                if (item != null)
                {
                    _logger.LogInformation("İçerik bulundu: {Content}", item.Content);
                    model.Content = item.Content;
                }
                else
                {
                    _logger.LogWarning("Bu kategori ve alt kategori için içerik bulunamadı: {Category}/{SubCategory} (Department: {Department})", 
                        selectedCategory, selectedSubCategory, department);
                }
            }
            
            return model;
        }
        
        /// <summary>
        /// Yeni içerik ekler
        /// </summary>
        public async Task AddNewContentAsync(NewContentViewModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            // Create the content item
            var newItem = new ContentItem
            {
                Category = model.Category.Trim(),
                SubCategory = model.SubCategory?.Trim(),
                Content = model.Content.Trim(),
                Department = model.Department // Include department
            };
            
            await _contentService.CreateAsync(newItem);
            
            // Invalidate category-related caches
            InvalidateCategoryCaches();
            
            _logger.LogInformation("Yeni içerik eklendi: {Category}/{SubCategory} (Department: {Department})", 
                model.Category, model.SubCategory, model.Department);
        }
        
        /// <summary>
        /// Var olan içeriği günceller
        /// </summary>
        public async Task UpdateContentAsync(ExtendContentViewModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            
            var chosenCategory = model.SelectedCategory?.Trim();
            var chosenSubCategory = model.SelectedSubCategory?.Trim();
            var newSubCategory = model.NewSubCategory?.Trim();
            
            // Get all items to work with
            var items = await _contentService.GetAllAsync(true);
            
            // Determine which subcategory to use
            string actualSubCategory = DetermineActualSubcategory(
                chosenCategory, chosenSubCategory, newSubCategory, items);
            
            // Handle category renaming if needed
            if (!string.IsNullOrWhiteSpace(model.EditedCategory) && 
                model.EditedCategory.Trim() != chosenCategory)
            {
                await UpdateCategoryNameInItems(items, chosenCategory, model.EditedCategory.Trim());
                chosenCategory = model.EditedCategory.Trim();
            }
            
            // Handle subcategory renaming if needed
            if (!string.IsNullOrWhiteSpace(model.EditedSubCategory) && 
                model.EditedSubCategory.Trim() != chosenSubCategory)
            {
                UpdateSubcategoryNameInItems(items, chosenCategory, chosenSubCategory, model.EditedSubCategory.Trim());
                actualSubCategory = model.EditedSubCategory.Trim();
            }
            
            // Create or update the content item
            await UpdateOrCreateContentItem(chosenCategory, actualSubCategory, model.Content?.Trim(), model.Department);
            
            // Invalidate category-related caches
            InvalidateCategoryCaches();
            
            _logger.LogInformation("İçerik güncellendi: {Category}/{SubCategory} (Department: {Department})", 
                chosenCategory, actualSubCategory, model.Department);
        }
        
        /// <summary>
        /// Kategori adını günceller
        /// </summary>
        public async Task UpdateCategoryAsync(string oldCategory, string newCategory)
        {
            if (string.IsNullOrWhiteSpace(oldCategory))
                throw new ArgumentException("Eski kategori adı boş olamaz", nameof(oldCategory));
                
            if (string.IsNullOrWhiteSpace(newCategory))
                throw new ArgumentException("Yeni kategori adı boş olamaz", nameof(newCategory));
                
            var items = await _contentService.GetAllAsync(true);
            await UpdateCategoryNameInItems(items, oldCategory, newCategory);
            
            _logger.LogInformation("Kategori güncellendi: {OldCategory} -> {NewCategory}", oldCategory, newCategory);
        }
        
        /// <summary>
        /// Alt kategori adını günceller
        /// </summary>
        public async Task UpdateSubCategoryAsync(string category, string oldSubCategory, string newSubCategory)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Kategori adı boş olamaz", nameof(category));
                
            if (string.IsNullOrWhiteSpace(oldSubCategory))
                throw new ArgumentException("Eski alt kategori adı boş olamaz", nameof(oldSubCategory));
                
            if (string.IsNullOrWhiteSpace(newSubCategory))
                throw new ArgumentException("Yeni alt kategori adı boş olamaz", nameof(newSubCategory));
                
            var items = await _contentService.GetAllAsync(true);
            
            // First check if the new subcategory already exists
            if (items.Any(x => 
                x.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                x.SubCategory?.Equals(newSubCategory, StringComparison.OrdinalIgnoreCase) == true))
            {
                throw new InvalidOperationException($"Bu alt kategori zaten mevcut: {category}/{newSubCategory}");
            }
            
            // Rename all matching subcategories
            UpdateSubcategoryNameInItems(items, category, oldSubCategory, newSubCategory);
            
            // Save all changes
            await _contentService.UpdateManyAsync(items);
            
            // Invalidate category-related caches
            InvalidateCategoryCaches();
            _cacheService.Remove($"{SUBCATEGORIES_CACHE_PREFIX}{category}");
            
            _logger.LogInformation("Alt kategori güncellendi: {Category}/{OldSubCategory} -> {Category}/{NewSubCategory}", 
                category, oldSubCategory, newSubCategory);
        }
        
        /// <summary>
        /// Yeni alt kategori ekler
        /// </summary>
        public async Task AddSubCategoryAsync(string category, string newSubCategory)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Kategori adı boş olamaz", nameof(category));
                
            if (string.IsNullOrWhiteSpace(newSubCategory))
                throw new ArgumentException("Yeni alt kategori adı boş olamaz", nameof(newSubCategory));
                
            // Create empty content for the new subcategory
            var newItem = new ContentItem
            {
                Category = category,
                SubCategory = newSubCategory,
                Content = string.Empty,
                Department = Department.Yazılım // Default to Software department, could be changed
            };
            
            await _contentService.CreateAsync(newItem);
            
            // Invalidate category-related caches
            _cacheService.Remove($"{SUBCATEGORIES_CACHE_PREFIX}{category}");
            
            _logger.LogInformation("Yeni alt kategori eklendi: {Category}/{SubCategory}", category, newSubCategory);
        }
        
        /// <summary>
        /// Kategoriyi ve ilgili tüm içerikleri siler
        /// </summary>
        public async Task<int> DeleteCategoryAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Kategori adı boş olamaz", nameof(category));
                
            int removedCount = await _contentService.DeleteByCategoryAsync(category);
            
            // Invalidate category-related caches
            InvalidateCategoryCaches();
            _cacheService.Remove($"{SUBCATEGORIES_CACHE_PREFIX}{category}");
            
            _logger.LogInformation("Kategori silindi: {Category}, {Count} öğe etkilendi", category, removedCount);
            
            return removedCount;
        }
        
        /// <summary>
        /// Tüm kategori isimlerini getirir
        /// </summary>
        public async Task<List<string>> GetAllCategoriesAsync()
        {
            return await _cacheService.GetOrSetAsync(CATEGORIES_CACHE_KEY, async () => 
            {
                var items = await _contentService.GetAllAsync();
                return GetDistinctOrderedCategories(items);
            }, TimeSpan.FromMinutes(30));
        }
        
        /// <summary>
        /// Belirli bir kategoriye ait alt kategorileri getirir
        /// </summary>
        public async Task<List<string>> GetSubcategoriesAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return new List<string>();
                
            return await _cacheService.GetOrSetAsync($"{SUBCATEGORIES_CACHE_PREFIX}{category}", async () => 
            {
                var items = await _contentService.GetAllAsync();
                return items
                    .Where(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.SubCategory)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();
            }, TimeSpan.FromMinutes(30));
        }
        
        #region Helper Methods
        
        /// <summary>
        /// Kategori önbelleklerini geçersiz kılar
        /// </summary>
        private void InvalidateCategoryCaches()
        {
            _cacheService.Remove(CATEGORIES_CACHE_KEY);
            _logger.LogDebug("Kategori önbellekleri temizlendi");
        }
        
        /// <summary>
        /// İçerik öğeleri içinden benzersiz ve sıralı kategorileri alır
        /// </summary>
        private List<string> GetDistinctOrderedCategories(List<ContentItem> items)
        {
            return items
                .Select(i => i.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();
        }
        
        /// <summary>
        /// Kategori-alt kategori sözlüğü oluşturur
        /// </summary>
        private Dictionary<string, List<string>> BuildSubcategoryDictionary(List<ContentItem> items)
        {
            return items
                .GroupBy(x => x.Category)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(i => i.SubCategory)
                          .Distinct(StringComparer.OrdinalIgnoreCase)
                          .OrderBy(s => s)
                          .ToList(),
                    StringComparer.OrdinalIgnoreCase
                );
        }
        
        /// <summary>
        /// Kategoriye göre içerik öğelerini filtreler
        /// </summary>
        private List<ContentItem> FilterItemsByCategory(List<ContentItem> items, string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return items;
                
            return items
                .Where(i => i.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        /// <summary>
        /// Departmana göre içerik öğelerini filtreler
        /// </summary>
        private List<ContentItem> FilterItemsByDepartment(List<ContentItem> items, Department department)
        {
            return items
                .Where(i => i.Department == department)
                .ToList();
        }
        
        /// <summary>
        /// Kullanılacak asıl alt kategoriyi belirler
        /// </summary>
        private string DetermineActualSubcategory(
            string chosenCategory, 
            string chosenSubCategory, 
            string newSubCategory,
            List<ContentItem> items)
        {
            // If new subcategory is provided, use that
            if (!string.IsNullOrWhiteSpace(newSubCategory))
                return newSubCategory;
                
            // If chosen subcategory is provided, use that
            if (!string.IsNullOrWhiteSpace(chosenSubCategory))
                return chosenSubCategory;
                
            // Try to find a default subcategory
            var defaultSub = items
                .FirstOrDefault(x => x.Category.Equals(chosenCategory, StringComparison.OrdinalIgnoreCase))
                ?.SubCategory;
                
            if (string.IsNullOrWhiteSpace(defaultSub))
                throw new InvalidOperationException(
                    "Lütfen mevcut bir alt kategori seçin veya yeni bir alt kategori ekleyin");
                    
            return defaultSub;
        }
        
        /// <summary>
        /// Tüm içerik öğeleri içinde kategori adını günceller
        /// </summary>
        private async Task UpdateCategoryNameInItems(List<ContentItem> items, string oldCategory, string newCategory)
        {
            // Check if the new category already exists
            if (items.Any(x => x.Category.Equals(newCategory, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Bu kategori zaten mevcut: {newCategory}");
                
            // Update all items with the matching category
            foreach (var item in items.Where(x => x.Category.Equals(oldCategory, StringComparison.OrdinalIgnoreCase)))
            {
                item.Category = newCategory;
            }
            
            // Save all changes
            await _contentService.UpdateManyAsync(items);
            
            // Invalidate category-related caches
            InvalidateCategoryCaches();
        }
        
        /// <summary>
        /// İçerik öğeleri içinde alt kategori adını günceller
        /// </summary>
        private void UpdateSubcategoryNameInItems(
            List<ContentItem> items, 
            string category, 
            string oldSubCategory, 
            string newSubCategory)
        {
            foreach (var item in items.Where(x =>
                x.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                x.SubCategory.Equals(oldSubCategory, StringComparison.OrdinalIgnoreCase)))
            {
                item.SubCategory = newSubCategory;
            }
        }
        
        /// <summary>
        /// İçerik öğesini günceller veya yeni oluşturur
        /// </summary>
        private async Task UpdateOrCreateContentItem(string category, string subcategory, string content, Department department)
        {
            // Try to get existing item
            var item = await _contentService.GetByCategoryAndSubcategoryAsync(category, subcategory, department);
            
            if (item != null)
            {
                // Update existing item
                item.Content = content ?? string.Empty;
                await _contentService.UpdateAsync(item);
            }
            else
            {
                // Create new item
                var newItem = new ContentItem
                {
                    Category = category,
                    SubCategory = subcategory,
                    Content = content ?? string.Empty,
                    Department = department
                };
                
                await _contentService.CreateAsync(newItem);
            }
        }
        
        #endregion
    }
}