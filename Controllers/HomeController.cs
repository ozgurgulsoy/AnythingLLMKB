using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TestKB.Models;
using TestKB.Services;
using TestKB.ViewModels;
using Microsoft.AspNetCore.Hosting;

namespace TestKB.Controllers
{
    /// <summary>
    /// Controller responsible for handling content management operations.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly IContentService _contentService;
        private readonly IWebHostEnvironment _env;
        private static readonly object _logLock = new object();

        /// <summary>
        /// Initializes a new instance of the HomeController.
        /// </summary>
        /// <param name="contentService">The content service dependency.</param>
        /// <param name="env">The hosting environment.</param>
        public HomeController(IContentService contentService, IWebHostEnvironment env)
        {
            _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        /// <summary>
        /// Renders the department selection view.
        /// </summary>
        /// <returns>The DepartmentSelect view.</returns>
        public IActionResult DepartmentSelect()
        {
            return View();
        }

        /// <summary>
        /// Redirects to the Index action after a department is selected.
        /// </summary>
        /// <param name="department">The selected department.</param>
        /// <returns>Redirect to Index action with department parameter.</returns>
        [HttpPost]
        public IActionResult SelectDepartment(Department department)
        {
            return RedirectToAction("Index", new { dept = department });
        }

        /// <summary>
        /// Retrieves content items and displays them filtered by category.
        /// </summary>
        /// <param name="category">The category filter.</param>
        /// <param name="dept">Optional department filter.</param>
        /// <returns>The Index view with a list of content items.</returns>
        public IActionResult Index(string category, Department? dept)
        {
            try
            {
                List<ContentItem> allItems = _contentService.GetContentItems();

                var allCategories = allItems
                    .Select(item => item.Category)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c)
                    .ToList();

                var filteredItems = string.IsNullOrWhiteSpace(category)
                    ? allItems
                    : allItems.Where(item =>
                        string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase))
                      .ToList();

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
                LogError($"Index action failed: {ex.Message}");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Retrieves the content item for editing based on selected category and subcategory.
        /// </summary>
        /// <param name="selectedCategory">The selected category.</param>
        /// <param name="selectedSubCategory">The selected subcategory.</param>
        /// <returns>The Edit view with pre-filled values if available.</returns>
        [HttpGet]
        public IActionResult Edit(string selectedCategory, string selectedSubCategory)
        {
            try
            {
                var extendModel = new ExtendContentViewModel();
                var items = _contentService.GetContentItems();

                if (!string.IsNullOrWhiteSpace(selectedCategory))
                {
                    extendModel.SelectedCategory = selectedCategory;
                }

                if (!string.IsNullOrWhiteSpace(selectedSubCategory))
                {
                    extendModel.SelectedSubCategory = selectedSubCategory;
                    var entry = items.FirstOrDefault(x =>
                        x.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase) &&
                        x.SubCategory.Equals(selectedSubCategory, StringComparison.OrdinalIgnoreCase));

                    if (entry != null)
                    {
                        extendModel.Content = entry.Content;
                    }
                }

                var viewModel = BuildEditContentViewModel(new NewContentViewModel(), extendModel);
                ViewBag.AllItemsJson = JsonSerializer.Serialize(items);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                LogError($"Edit GET action failed: {ex.Message}");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Adds a new content item based on the provided model.
        /// </summary>
        /// <param name="model">The new content view model.</param>
        /// <returns>Redirects to the Edit view if successful, otherwise returns the view with errors.</returns>
        [HttpPost]
        public IActionResult EditNewContent([Bind(Prefix = "NewContent")] NewContentViewModel model)
        {
            try
            {
                var allItems = _contentService.GetContentItems();

                // Check if category already exists.
                bool categoryExists = allItems.Any(x =>
                    x.Category.Equals(model.Category?.Trim(), StringComparison.OrdinalIgnoreCase));

                if (categoryExists)
                {
                    ModelState.AddModelError("NewContent.Category", "Bu kategori zaten mevcut!");
                }

                if (!string.IsNullOrWhiteSpace(model.SubCategory))
                {
                    bool subCatExists = allItems.Any(x =>
                        x.Category.Equals(model.Category?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                        x.SubCategory.Equals(model.SubCategory?.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (subCatExists)
                    {
                        ModelState.AddModelError("NewContent.SubCategory", "Bu alt kategori zaten mevcut!");
                    }
                }

                if (ModelState.IsValid)
                {
                    _contentService.AddNewContent(model.Category, model.SubCategory, model.Content);
                    TempData["SuccessMessage"] = "İçerik eklendi.";
                    return RedirectToAction("Edit");
                }

                var vm = BuildEditContentViewModel(model, new ExtendContentViewModel());
                vm = AddAllItemsJson(vm);
                return View("Edit", vm);
            }
            catch (Exception ex)
            {
                LogError($"EditNewContent action failed: {ex.Message}");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Extends or updates an existing content item.
        /// </summary>
        /// <param name="model">The view model containing updated content data.</param>
        /// <returns>Redirects to the Edit view if successful, otherwise returns the view with errors.</returns>
        [HttpPost]
        public IActionResult ExtendContent([Bind(Prefix = "ExtendContent")] ExtendContentViewModel model)
        {
            try
            {
                // If the content is empty, add a model error.
                if (string.IsNullOrWhiteSpace(model.Content))
                {
                    ModelState.AddModelError("ExtendContent.Content", "İçerik boş bırakılamaz.");
                }

                var items = _contentService.GetContentItems();
                var chosenCategory = model.SelectedCategory?.Trim();
                var chosenSubCategory = model.SelectedSubCategory?.Trim();
                var newSubCat = model.NewSubCategory?.Trim();

                // Use newSubCat if provided; otherwise use chosenSubCategory.
                var actualSubCategory = !string.IsNullOrWhiteSpace(newSubCat) ? newSubCat : chosenSubCategory;

                // Default subcategory handling.
                if (string.IsNullOrWhiteSpace(chosenSubCategory) && string.IsNullOrWhiteSpace(newSubCat))
                {
                    var defaultSub = items.FirstOrDefault(x =>
                        x.Category.Equals(chosenCategory, StringComparison.OrdinalIgnoreCase))?.SubCategory;

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

                // Check if the new subcategory already exists.
                if (!string.IsNullOrWhiteSpace(newSubCat))
                {
                    bool subCatExists = items.Any(x =>
                        x.Category.Equals(chosenCategory, StringComparison.OrdinalIgnoreCase) &&
                        x.SubCategory.Equals(actualSubCategory, StringComparison.OrdinalIgnoreCase));

                    if (subCatExists)
                    {
                        ModelState.AddModelError("ExtendContent.NewSubCategory", "Bu alt kategori zaten mevcut!");
                    }
                }

                if (!ModelState.IsValid)
                {
                    var vm = BuildEditContentViewModel(new NewContentViewModel(), model);
                    ViewBag.AllItemsJson = JsonSerializer.Serialize(items);
                    return View("Edit", vm);
                }

                // Update category if EditedCategory is provided.
                if (!string.IsNullOrWhiteSpace(model.EditedCategory) && model.EditedCategory.Trim() != chosenCategory)
                {
                    string newCategory = model.EditedCategory.Trim();
                    foreach (var item in items.Where(x =>
                        x.Category.Equals(chosenCategory, StringComparison.OrdinalIgnoreCase)))
                    {
                        item.Category = newCategory;
                    }
                    chosenCategory = newCategory;
                }

                // Update subcategory if EditedSubCategory is provided.
                if (!string.IsNullOrWhiteSpace(model.EditedSubCategory) && model.EditedSubCategory.Trim() != chosenSubCategory)
                {
                    string newSubCategory = model.EditedSubCategory.Trim();
                    foreach (var item in items.Where(x =>
                        x.Category.Equals(chosenCategory, StringComparison.OrdinalIgnoreCase) &&
                        x.SubCategory.Equals(chosenSubCategory, StringComparison.OrdinalIgnoreCase)))
                    {
                        item.SubCategory = newSubCategory;
                    }
                    chosenSubCategory = newSubCategory;
                    actualSubCategory = newSubCategory;
                }

                // Update existing entry or add a new one.
                var existingEntry = items.FirstOrDefault(x =>
                    x.Category.Equals(chosenCategory, StringComparison.OrdinalIgnoreCase) &&
                    x.SubCategory.Equals(actualSubCategory, StringComparison.OrdinalIgnoreCase));

                if (existingEntry != null)
                {
                    existingEntry.Content = model.Content?.Trim();
                }
                else
                {
                    items.Add(new ContentItem
                    {
                        Category = chosenCategory,
                        SubCategory = actualSubCategory,
                        Content = model.Content?.Trim()
                    });
                }

                _contentService.UpdateContentItems(items);
                TempData["SuccessMessage"] = "İçerik güncellendi.";
                return RedirectToAction("Edit");
            }
            catch (Exception ex)
            {
                LogError($"ExtendContent action failed: {ex.Message}");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Updates the category name for all content items with a given old category.
        /// </summary>
        /// <param name="model">The update category view model containing old and new category names.</param>
        /// <returns>A JSON result indicating success or failure.</returns>
        [HttpPost]
        public IActionResult UpdateCategory([FromBody] UpdateCategoryViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.OldCategory) || string.IsNullOrWhiteSpace(model.NewCategory))
                {
                    return Json(new { success = false, message = "Eski veya yeni kategori ismi boş olamaz." });
                }

                var items = _contentService.GetContentItems();
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
                LogError($"UpdateCategory action failed: {ex.Message}");
                return Json(new { success = false, message = "Kategori güncellenirken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Updates the subcategory name for content items under a specific category.
        /// </summary>
        /// <param name="model">The update subcategory view model containing category, old, and new subcategory names.</param>
        /// <returns>A JSON result indicating success or failure.</returns>
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

                var items = _contentService.GetContentItems();
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
                LogError($"UpdateSubCategory action failed: {ex.Message}");
                return Json(new { success = false, message = "Alt kategori güncellenirken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Retrieves all content items as JSON.
        /// </summary>
        /// <returns>A JSON result containing all content items.</returns>
        [HttpGet]
        public IActionResult GetContentItems()
        {
            try
            {
                var items = _contentService.GetContentItems();
                return Json(items);
            }
            catch (Exception ex)
            {
                LogError($"GetContentItems action failed: {ex.Message}");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Converts the JSON data to plain text and renders it.
        /// </summary>
        /// <returns>The ConvertedContent view or a content result if the JSON file is missing.</returns>
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
                LogError($"ConvertedContent action failed: {ex.Message}");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Builds the view model used in the Edit view with current content data.
        /// </summary>
        /// <param name="newContent">The new content view model.</param>
        /// <param name="extendContent">The extend content view model.</param>
        /// <returns>An instance of EditContentViewModel.</returns>
        private EditContentViewModel BuildEditContentViewModel(NewContentViewModel newContent, ExtendContentViewModel extendContent)
        {
            var allItems = _contentService.GetContentItems();

            var categories = allItems
                .Select(x => x.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

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

        /// <summary>
        /// Adds a JSON representation of all content items to the view model via ViewBag.
        /// </summary>
        /// <param name="vm">The edit content view model.</param>
        /// <returns>The updated view model.</returns>
        private EditContentViewModel AddAllItemsJson(EditContentViewModel vm)
        {
            var allItems = _contentService.GetContentItems();
            ViewBag.AllItemsJson = JsonSerializer.Serialize(allItems);
            return vm;
        }

        /// <summary>
        /// Deletes all content items under a specified category.
        /// </summary>
        /// <param name="model">The view model containing the category to delete.</param>
        /// <returns>A JSON result indicating how many items were removed.</returns>
        [HttpPost]
        public IActionResult DeleteCategory([FromBody] DeleteCategoryViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Category))
                {
                    return Json(new { success = false, message = "Kategori ismi boş olamaz." });
                }

                var items = _contentService.GetContentItems();
                var categoryToDelete = model.Category.Trim();

                int removedCount = items.RemoveAll(x =>
                    x.Category.Equals(categoryToDelete, StringComparison.OrdinalIgnoreCase));

                _contentService.UpdateContentItems(items);
                return Json(new { success = true, message = $"{removedCount} içerik silindi." });
            }
            catch (Exception ex)
            {
                LogError($"DeleteCategory action failed: {ex.Message}");
                return Json(new { success = false, message = "Kategori silinirken bir hata oluştu." });
            }
        }

        /// <summary>
        /// Logs error messages to the error output.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        private void LogError(string message)
        {
            lock (_logLock)
            {
                Console.Error.WriteLine($"[ERROR] {DateTime.UtcNow}: {message}");
            }
        }
    }
}
