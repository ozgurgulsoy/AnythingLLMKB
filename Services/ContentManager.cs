using System;
using System.Collections.Generic;
using System.Linq;
using TestKB.Models;
using TestKB.ViewModels;

namespace TestKB.Services
{
    /// <summary>
    /// İçerik verilerini işlemek ve yönetmek için kullanılan servis.
    /// </summary>
    public class ContentManager : IContentManager
    {
        private readonly IContentService _contentService;

        /// <summary>
        /// ContentManager sınıfının yapıcı metodu.
        /// </summary>
        /// <param name="contentService">İçerik servisi bağımlılığı</param>
        public ContentManager(IContentService contentService)
        {
            _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
        }

        /// <summary>
        /// Tüm içerik öğelerini getirir.
        /// </summary>
        public List<ContentItem> GetAllContentItems() =>
    _contentService.GetContentItemsAsync(true).Result;

        /// <summary>
        /// Verilen kategoriye göre içerik listesini ve kategorileri hazırlayan view modeli oluşturur.
        /// </summary>
        /// <param name="category">Filtrelenecek kategori</param>
        public ContentListViewModel BuildContentListViewModel(string category)
        {
            var items = GetAllContentItems();
            var categories = GetDistinctOrderedCategories(items);
            var filteredItems = FilterItemsByCategory(items, category);
            return new ContentListViewModel
            {
                ContentItems = filteredItems,
                AllCategories = categories,
                SelectedCategory = category
            };
        }

        /// <summary>
        /// Düzenleme ekranı için view modeli oluşturur.
        /// </summary>
        /// <param name="newContent">Yeni içerik view modeli</param>
        /// <param name="extendContent">Genişletilmiş içerik view modeli</param>
        public EditContentViewModel BuildEditContentViewModel(NewContentViewModel newContent, ExtendContentViewModel extendContent)
        {
            var items = GetAllContentItems();
            var categories = GetDistinctOrderedCategories(items);
            var subCategories = items.GroupBy(x => x.Category)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.SubCategory)
                          .Distinct(StringComparer.OrdinalIgnoreCase)
                          .OrderBy(s => s)
                          .ToList()
                );

            return new EditContentViewModel
            {
                ExistingCategories = categories,
                ExistingSubCategories = subCategories,
                NewContent = newContent,
                ExtendContent = extendContent
            };
        }

        /// <summary>
        /// Seçilen kategori ve alt kategoriye göre genişletilmiş içerik view modelini oluşturur.
        /// </summary>
        /// <param name="selectedCategory">Seçilen kategori</param>
        /// <param name="selectedSubCategory">Seçilen alt kategori</param>
        public ExtendContentViewModel CreateExtendContentViewModel(string selectedCategory, string selectedSubCategory)
        {
            var items = GetAllContentItems();
            var model = new ExtendContentViewModel
            {
                SelectedCategory = selectedCategory,
                SelectedSubCategory = selectedSubCategory
            };

            if (!string.IsNullOrWhiteSpace(selectedSubCategory))
            {
                var entry = items.FirstOrDefault(x =>
                    x.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase) &&
                    x.SubCategory.Equals(selectedSubCategory, StringComparison.OrdinalIgnoreCase));

                if (entry != null)
                {
                    model.Content = entry.Content;
                }
            }
            return model;
        }

        /// <summary>
        /// Yeni içerik ekler. Eğer kategori veya alt kategori varsa hata fırlatır.
        /// </summary>
        /// <param name="model">Yeni içerik view modeli</param>
        public void AddNewContent(NewContentViewModel model)
        {
            var items = GetAllContentItems();
            if (CategoryExists(items, model.Category?.Trim()))
            {
                throw new InvalidOperationException("This category already exists.");
            }
            if (!string.IsNullOrWhiteSpace(model.SubCategory) &&
                SubcategoryExists(items, model.Category?.Trim(), model.SubCategory?.Trim()))
            {
                throw new InvalidOperationException("This subcategory already exists.");
            }
            _contentService.AddNewContentAsync(model.Category.Trim(), model.SubCategory?.Trim(), model.Content.Trim());
        }

        /// <summary>
        /// Varolan içeriği günceller veya ekler.
        /// </summary>
        /// <param name="model">Genişletilmiş içerik view modeli</param>
        public void UpdateContent(ExtendContentViewModel model)
        {
            var items = GetAllContentItems();
            var chosenCategory = model.SelectedCategory?.Trim();
            var chosenSubCategory = model.SelectedSubCategory?.Trim();
            var newSubCategory = model.NewSubCategory?.Trim();

            // Alt kategori seçimini normalize eder.
            var actualSubCategory = NormalizeSubcategorySelection(ref chosenSubCategory, newSubCategory, chosenCategory, items);

            if (string.IsNullOrWhiteSpace(chosenSubCategory) && string.IsNullOrWhiteSpace(newSubCategory))
            {
                actualSubCategory = TryFindDefaultSubcategory(items, chosenCategory, out chosenSubCategory);
                if (string.IsNullOrWhiteSpace(chosenSubCategory))
                {
                    throw new InvalidOperationException("Please select an existing subcategory or enter a new one.");
                }
            }

            // Kategori yeniden adlandırılması gerekiyorsa günceller.
            if (!string.IsNullOrWhiteSpace(model.EditedCategory) && model.EditedCategory.Trim() != chosenCategory)
            {
                chosenCategory = RenameCategoryInAllItems(items, chosenCategory, model.EditedCategory.Trim());
            }

            // Alt kategori yeniden adlandırılması gerekiyorsa günceller.
            if (!string.IsNullOrWhiteSpace(model.EditedSubCategory) && model.EditedSubCategory.Trim() != chosenSubCategory)
            {
                actualSubCategory = RenameSubcategoryInItems(items, chosenCategory, chosenSubCategory, model.EditedSubCategory.Trim());
                chosenSubCategory = actualSubCategory;
            }

            UpdateOrCreateContentItem(items, chosenCategory, actualSubCategory, model.Content?.Trim());
            _contentService.UpdateContentItemsAsync(items);
        }

        /// <summary>
        /// Belirtilen kategoriyi günceller.
        /// </summary>
        /// <param name="oldCategory">Eski kategori adı</param>
        /// <param name="newCategory">Yeni kategori adı</param>
        public void UpdateCategory(string oldCategory, string newCategory)
        {
            var items = GetAllContentItems();
            oldCategory = oldCategory.Trim();
            newCategory = newCategory.Trim();
            if (items.Any(x => x.Category.Equals(newCategory, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("This category already exists.");
            }
            foreach (var item in items.Where(x => x.Category.Equals(oldCategory, StringComparison.OrdinalIgnoreCase)))
            {
                item.Category = newCategory;
            }
            _contentService.UpdateContentItemsAsync(items);
        }

        /// <summary>
        /// Belirtilen alt kategoriyi günceller.
        /// </summary>
        /// <param name="category">Kategori adı</param>
        /// <param name="oldSubCategory">Eski alt kategori adı</param>
        /// <param name="newSubCategory">Yeni alt kategori adı</param>
        public void UpdateSubCategory(string category, string oldSubCategory, string newSubCategory)
        {
            var items = GetAllContentItems();
            category = category.Trim();
            oldSubCategory = oldSubCategory.Trim();
            newSubCategory = newSubCategory.Trim();
            if (items.Any(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                                x.SubCategory.Equals(newSubCategory, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("This subcategory already exists.");
            }
            foreach (var item in items.Where(x =>
                x.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                x.SubCategory.Equals(oldSubCategory, StringComparison.OrdinalIgnoreCase)))
            {
                item.SubCategory = newSubCategory;
            }
            _contentService.UpdateContentItemsAsync(items);
        }

        /// <summary>
        /// Yeni alt kategori ekler.
        /// </summary>
        /// <param name="category">Kategori adı</param>
        /// <param name="newSubCategory">Yeni alt kategori adı</param>
        public void AddSubCategory(string category, string newSubCategory)
        {
            var items = GetAllContentItems();
            category = category.Trim();
            newSubCategory = newSubCategory.Trim();
            if (SubcategoryExists(items, category, newSubCategory))
            {
                throw new InvalidOperationException("This subcategory already exists.");
            }
            items.Add(new ContentItem
            {
                Category = category,
                SubCategory = newSubCategory,
                Content = string.Empty
            });
            _contentService.UpdateContentItemsAsync(items);
        }

        /// <summary>
        /// Belirtilen kategoriye ait tüm içerik öğelerini siler.
        /// </summary>
        /// <param name="category">Silinecek kategori adı</param>
        /// <returns>Silinen öğe sayısı</returns>
        public int DeleteCategory(string category)
        {
            var items = GetAllContentItems();
            category = category.Trim();
            int removedCount = items.RemoveAll(x =>
                x.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            _contentService.UpdateContentItemsAsync(items);
            return removedCount;
        }

        #region Yardımcı Metotlar

        /// <summary>
        /// İçerik öğeleri arasından benzersiz ve sıralı kategorileri döndürür.
        /// </summary>
        private List<string> GetDistinctOrderedCategories(List<ContentItem> items) =>
            items.Select(item => item.Category)
                 .Distinct(StringComparer.OrdinalIgnoreCase)
                 .OrderBy(c => c)
                 .ToList();

        /// <summary>
        /// Verilen kategoriye göre içerik öğelerini filtreler.
        /// </summary>
        private List<ContentItem> FilterItemsByCategory(List<ContentItem> items, string category) =>
            string.IsNullOrWhiteSpace(category)
                ? items
                : items.Where(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();

        /// <summary>
        /// Belirtilen kategorinin var olup olmadığını kontrol eder.
        /// </summary>
        private bool CategoryExists(List<ContentItem> items, string category) =>
            !string.IsNullOrWhiteSpace(category) && items.Any(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Belirtilen alt kategorinin var olup olmadığını kontrol eder.
        /// </summary>
        private bool SubcategoryExists(List<ContentItem> items, string category, string subcategory) =>
            !string.IsNullOrWhiteSpace(category) &&
            !string.IsNullOrWhiteSpace(subcategory) &&
            items.Any(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                           x.SubCategory.Equals(subcategory, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Alt kategori seçimini normalize eder.
        /// </summary>
        private string NormalizeSubcategorySelection(ref string chosenSubCategory, string newSubCat, string chosenCategory, List<ContentItem> items)
        {
            if (!string.IsNullOrWhiteSpace(newSubCat) &&
                !string.IsNullOrWhiteSpace(chosenSubCategory) &&
                newSubCat.Equals(chosenSubCategory, StringComparison.OrdinalIgnoreCase))
            {
                return chosenSubCategory;
            }
            return !string.IsNullOrWhiteSpace(newSubCat) ? newSubCat : chosenSubCategory;
        }

        /// <summary>
        /// Varsayılan alt kategoriyi bulmaya çalışır.
        /// </summary>
        private string TryFindDefaultSubcategory(List<ContentItem> items, string category, out string chosenSubCategory)
        {
            var defaultSub = items.FirstOrDefault(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase))?.SubCategory;
            chosenSubCategory = defaultSub;
            return defaultSub;
        }

        /// <summary>
        /// Tüm içerik öğeleri içinde kategori yeniden adlandırmasını yapar.
        /// </summary>
        private string RenameCategoryInAllItems(List<ContentItem> items, string oldCategory, string newCategory)
        {
            foreach (var item in items.Where(x => x.Category.Equals(oldCategory, StringComparison.OrdinalIgnoreCase)))
            {
                item.Category = newCategory;
            }
            return newCategory;
        }

        /// <summary>
        /// Tüm içerik öğeleri içinde alt kategori yeniden adlandırmasını yapar.
        /// </summary>
        private string RenameSubcategoryInItems(List<ContentItem> items, string category, string oldSubCategory, string newSubCategory)
        {
            foreach (var item in items.Where(x =>
                x.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                x.SubCategory.Equals(oldSubCategory, StringComparison.OrdinalIgnoreCase)))
            {
                item.SubCategory = newSubCategory;
            }
            return newSubCategory;
        }

        /// <summary>
        /// Mevcut içerik öğesini günceller ya da yoksa yeni öğe ekler.
        /// </summary>
        private void UpdateOrCreateContentItem(List<ContentItem> items, string category, string subcategory, string content)
        {
            var existingEntries = items
                .Where(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                            x.SubCategory.Equals(subcategory, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (existingEntries.Any())
            {
                existingEntries[0].Content = content;
                foreach (var duplicate in existingEntries.Skip(1).ToList())
                {
                    items.Remove(duplicate);
                }
            }
            else
            {
                items.Add(new ContentItem
                {
                    Category = category,
                    SubCategory = subcategory,
                    Content = content
                });
            }
        }

        #endregion
    }
}
