using Microsoft.AspNetCore.Mvc;

using System.Text.Json;
using TestKB.Models;
using TestKB.Services;
using TestKB.ViewModels;


namespace TestKB.Controllers
{
    public class HomeController : Controller
    {
        private readonly IContentService _contentService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IContentService contentService, IWebHostEnvironment env, ILogger<HomeController> logger)
        {
            _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IActionResult DepartmentSelect() => View();

        [HttpPost]
        public IActionResult SelectDepartment(Department department) =>
            RedirectToAction("Index", new { dept = department });

        public IActionResult Index(string category, Department? dept)
        {
            try
            {
                List<ContentItem> allItems = _contentService.GetContentItems(true);

                var allCategories = GetDistinctOrderedCategories(allItems);

                var filteredItems = FilterItemsByCategory(allItems, category);

                var viewModel = new ContentListViewModel
                {
                    ContentItems = filteredItems,
                    AllCategories = allCategories,
                    SelectedCategory = category
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Index action failed");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet]
        public IActionResult Edit(string selectedCategory, string selectedSubCategory)
        {
            try
            {
                var items = _contentService.GetContentItems(true);
                var extendModel = CreateExtendContentViewModel(selectedCategory, selectedSubCategory, items);
                var viewModel = BuildEditContentViewModel(new NewContentViewModel(), extendModel);

                ViewBag.AllItemsJson = JsonSerializer.Serialize(items);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Edit GET action failed");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost]
        public IActionResult EditNewContent([Bind(Prefix = "NewContent")] NewContentViewModel model)
        {
            try
            {
                var allItems = _contentService.GetContentItems(true);

                ValidateNewContentModel(model, allItems);

                if (ModelState.IsValid)
                {
                    _contentService.AddNewContent(model.Category, model.SubCategory, model.Content);
                    TempData["SuccessMessage"] = "İçerik eklendi.";
                    return RedirectToAction("Edit");
                }

                var vm = BuildEditContentViewModel(model, new ExtendContentViewModel());
                ViewBag.AllItemsJson = JsonSerializer.Serialize(allItems);
                return View("Edit", vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EditNewContent action failed");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost]
        public IActionResult ExtendContent([Bind(Prefix = "ExtendContent")] ExtendContentViewModel model)
        {
            try
            {
                _logger.LogDebug("ExtendContent params: Category={Category}, SubCategory={SubCategory}, NewSubCategory={NewSubCategory}",
                                model.SelectedCategory, model.SelectedSubCategory, model.NewSubCategory);

                if (string.IsNullOrWhiteSpace(model.Content))
                {
                    ModelState.AddModelError("ExtendContent.Content", "İçerik boş bırakılamaz.");
                }

                var items = _contentService.GetContentItems(true);
                var chosenCategory = model.SelectedCategory?.Trim();
                var chosenSubCategory = model.SelectedSubCategory?.Trim();
                var newSubCat = model.NewSubCategory?.Trim();

                // Normalize input data
                var actualSubCategory = NormalizeSubcategorySelection(ref chosenSubCategory, newSubCat, chosenCategory, items);

                // Validate subcategory selection
                if (string.IsNullOrWhiteSpace(chosenSubCategory) && string.IsNullOrWhiteSpace(newSubCat))
                {
                    TryFindDefaultSubcategory(items, chosenCategory, ref chosenSubCategory, ref actualSubCategory, model);
                }

                // Validate new subcategory for duplicates
                ValidateNewSubcategory(newSubCat, chosenCategory, items);

                if (!ModelState.IsValid)
                {
                    LogModelStateErrors();
                    var vm = BuildEditContentViewModel(new NewContentViewModel(), model);
                    ViewBag.AllItemsJson = JsonSerializer.Serialize(items);
                    return View("Edit", vm);
                }

                // Handle category rename if needed
                if (ShouldRenameCategory(model, chosenCategory))
                {
                    chosenCategory = RenameCategoryInAllItems(items, chosenCategory, model.EditedCategory.Trim());
                }

                // Handle subcategory rename if needed
                if (ShouldRenameSubcategory(model, chosenSubCategory))
                {
                    actualSubCategory = RenameSubcategoryInItems(items, chosenCategory, chosenSubCategory, model.EditedSubCategory.Trim());
                    chosenSubCategory = actualSubCategory;
                }

                UpdateOrCreateContentItem(items, chosenCategory, actualSubCategory, model.Content?.Trim());

                _contentService.UpdateContentItems(items);
                TempData["SuccessMessage"] = "İçerik güncellendi.";
                return RedirectToAction("Edit");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExtendContent action failed");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost]
        public IActionResult UpdateCategory([FromBody] UpdateCategoryViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.OldCategory) || string.IsNullOrWhiteSpace(model.NewCategory))
                {
                    return Json(new { success = false, message = "Eski veya yeni kategori ismi boş olamaz." });
                }

                var items = _contentService.GetContentItems(true);
                var oldCat = model.OldCategory.Trim();
                var newCat = model.NewCategory.Trim();

                if (items.Any(x => x.Category.Equals(newCat, StringComparison.OrdinalIgnoreCase)))
                {
                    return Json(new { success = false, message = "Bu kategori zaten mevcut." });
                }

                foreach (var item in items.Where(x => x.Category.Equals(oldCat, StringComparison.OrdinalIgnoreCase)))
                {
                    item.Category = newCat;
                }

                _contentService.UpdateContentItems(items);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateCategory action failed");
                return Json(new { success = false, message = "Kategori güncellenirken bir hata oluştu." });
            }
        }

        [HttpPost]
        public IActionResult UpdateSubCategory([FromBody] UpdateSubCategoryViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Category)
                    || string.IsNullOrWhiteSpace(model.OldSubCategory)
                    || string.IsNullOrWhiteSpace(model.NewSubCategory))
                {
                    return Json(new { success = false, message = "Kategori veya alt kategori bilgisi boş olamaz." });
                }

                var items = _contentService.GetContentItems(true);
                var cat = model.Category.Trim();
                var oldSub = model.OldSubCategory.Trim();
                var newSub = model.NewSubCategory.Trim();

                if (items.Any(x =>
                    x.Category.Equals(cat, StringComparison.OrdinalIgnoreCase) &&
                    x.SubCategory.Equals(newSub, StringComparison.OrdinalIgnoreCase)))
                {
                    return Json(new { success = false, message = "Bu alt kategori zaten mevcut." });
                }

                foreach (var item in items.Where(x =>
                    x.Category.Equals(cat, StringComparison.OrdinalIgnoreCase) &&
                    x.SubCategory.Equals(oldSub, StringComparison.OrdinalIgnoreCase)))
                {
                    item.SubCategory = newSub;
                }

                _contentService.UpdateContentItems(items);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateSubCategory action failed");
                return Json(new { success = false, message = "Alt kategori güncellenirken bir hata oluştu." });
            }
        }

        [HttpPost]
        public IActionResult AddSubCategory([FromBody] AddSubCategoryViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Category) || string.IsNullOrWhiteSpace(model.NewSubCategory))
                {
                    return Json(new { success = false, message = "Kategori veya yeni alt kategori bilgisi boş olamaz." });
                }

                var items = _contentService.GetContentItems(true);
                var category = model.Category.Trim();
                var newSub = model.NewSubCategory.Trim();

                if (SubcategoryExists(items, category, newSub))
                {
                    return Json(new { success = false, message = "Bu alt kategori zaten mevcut." });
                }

                // Add new entry with empty content for the new subcategory.
                items.Add(new ContentItem
                {
                    Category = category,
                    SubCategory = newSub,
                    Content = ""
                });

                _contentService.UpdateContentItems(items);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddSubCategory action failed");
                return Json(new { success = false, message = "Yeni alt kategori eklenirken bir hata oluştu." });
            }
        }

        [HttpGet]
        public IActionResult GetContentItems()
        {
            try
            {
                var items = _contentService.GetContentItems(true);
                return Json(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetContentItems action failed");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        public IActionResult ConvertedContent()
        {
            try
            {
                string jsonFilePath = Path.Combine(_env.WebRootPath, "data.json");
                if (!System.IO.File.Exists(jsonFilePath))
                {
                    return Content("JSON file not found.");
                }

                string jsonContent = System.IO.File.ReadAllText(jsonFilePath);
                string plainText = TestKB.Services.JsonToTextConverter.ConvertJsonToText(jsonContent);
                return View((object)plainText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConvertedContent action failed");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost]
        public IActionResult DeleteCategory([FromBody] DeleteCategoryViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Category))
                {
                    return Json(new { success = false, message = "Kategori ismi boş olamaz." });
                }

                var items = _contentService.GetContentItems(true);
                var categoryToDelete = model.Category.Trim();

                int removedCount = items.RemoveAll(x =>
                    x.Category.Equals(categoryToDelete, StringComparison.OrdinalIgnoreCase));

                _contentService.UpdateContentItems(items);
                return Json(new { success = true, message = $"{removedCount} içerik silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteCategory action failed");
                return Json(new { success = false, message = "Kategori silinirken bir hata oluştu." });
            }
        }

        #region Helper Methods
        private List<string> GetDistinctOrderedCategories(List<ContentItem> items)
        {
            return items
                .Select(item => item.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();
        }

        private List<ContentItem> FilterItemsByCategory(List<ContentItem> allItems, string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return allItems;
            }

            return allItems.Where(item =>
                string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private ExtendContentViewModel CreateExtendContentViewModel(
            string selectedCategory,
            string selectedSubCategory,
            List<ContentItem> items)
        {
            var model = new ExtendContentViewModel();

            if (!string.IsNullOrWhiteSpace(selectedCategory))
            {
                model.SelectedCategory = selectedCategory;
            }

            if (!string.IsNullOrWhiteSpace(selectedSubCategory))
            {
                model.SelectedSubCategory = selectedSubCategory;
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

        private void ValidateNewContentModel(NewContentViewModel model, List<ContentItem> allItems)
        {
            if (CategoryExists(allItems, model.Category?.Trim()))
            {
                ModelState.AddModelError("NewContent.Category", "Bu kategori zaten mevcut!");
            }

            if (!string.IsNullOrWhiteSpace(model.SubCategory) &&
                SubcategoryExists(allItems, model.Category?.Trim(), model.SubCategory?.Trim()))
            {
                ModelState.AddModelError("NewContent.SubCategory", "Bu alt kategori zaten mevcut!");
            }
        }

        private bool CategoryExists(List<ContentItem> items, string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return false;

            return items.Any(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        private bool SubcategoryExists(List<ContentItem> items, string category, string subcategory)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(subcategory))
                return false;

            return items.Any(x =>
                x.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                x.SubCategory.Equals(subcategory, StringComparison.OrdinalIgnoreCase));
        }

        private string NormalizeSubcategorySelection(
            ref string chosenSubCategory,
            string newSubCat,
            string chosenCategory,
            List<ContentItem> items)
        {
            // If the "new" subcategory value is the same as the selected one, ignore it
            if (!string.IsNullOrWhiteSpace(newSubCat) &&
                !string.IsNullOrWhiteSpace(chosenSubCategory) &&
                newSubCat.Equals(chosenSubCategory, StringComparison.OrdinalIgnoreCase))
            {
                return chosenSubCategory;
            }

            // Determine the actual subcategory to use
            return !string.IsNullOrWhiteSpace(newSubCat) ? newSubCat : chosenSubCategory;
        }

        private void TryFindDefaultSubcategory(
            List<ContentItem> items,
            string chosenCategory,
            ref string chosenSubCategory,
            ref string actualSubCategory,
            ExtendContentViewModel model)
        {
            var defaultSub = items
                .FirstOrDefault(x => x.Category.Equals(chosenCategory, StringComparison.OrdinalIgnoreCase))
                ?.SubCategory;

            if (!string.IsNullOrWhiteSpace(defaultSub))
            {
                chosenSubCategory = defaultSub;
                model.SelectedSubCategory = defaultSub;
                actualSubCategory = defaultSub;
            }
            else
            {
                ModelState.AddModelError("ExtendContent.SelectedSubCategory",
                    "Lütfen mevcut bir alt kategori seçiniz veya yeni bir alt kategori giriniz.");
            }
        }

        private void ValidateNewSubcategory(string newSubCat, string chosenCategory, List<ContentItem> items)
        {
            if (string.IsNullOrWhiteSpace(newSubCat))
                return;

            if (SubcategoryExists(items, chosenCategory, newSubCat))
            {
                ModelState.AddModelError("ExtendContent.NewSubCategory", "Bu alt kategori zaten mevcut!");
            }
        }

        private void LogModelStateErrors()
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));

            _logger.LogWarning("ModelState validation errors: {Errors}", errors);
        }

        private bool ShouldRenameCategory(ExtendContentViewModel model, string chosenCategory)
        {
            return !string.IsNullOrWhiteSpace(model.EditedCategory) &&
                   model.EditedCategory.Trim() != chosenCategory;
        }

        private string RenameCategoryInAllItems(List<ContentItem> items, string oldCategory, string newCategory)
        {
            foreach (var item in items.Where(x =>
                x.Category.Equals(oldCategory, StringComparison.OrdinalIgnoreCase)))
            {
                item.Category = newCategory;
            }

            return newCategory;
        }

        private bool ShouldRenameSubcategory(ExtendContentViewModel model, string chosenSubCategory)
        {
            return !string.IsNullOrWhiteSpace(model.EditedSubCategory) &&
                   model.EditedSubCategory.Trim() != chosenSubCategory;
        }

        private string RenameSubcategoryInItems(
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

            return newSubCategory;
        }

        private void UpdateOrCreateContentItem(
            List<ContentItem> items,
            string category,
            string subcategory,
            string content)
        {
            var existingEntry = items.FirstOrDefault(x =>
                x.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                x.SubCategory.Equals(subcategory, StringComparison.OrdinalIgnoreCase));

            if (existingEntry != null)
            {
                existingEntry.Content = content;
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

        private EditContentViewModel BuildEditContentViewModel(
            NewContentViewModel newContent,
            ExtendContentViewModel extendContent)
        {
            var allItems = _contentService.GetContentItems(true);

            var categories = GetDistinctOrderedCategories(allItems);

            var subCategories = allItems
                .GroupBy(x => x.Category)
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
        #endregion
    }
}