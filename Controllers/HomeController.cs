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

        public HomeController(IContentService contentService, IWebHostEnvironment env)
        {
            _contentService = contentService;
            _env = env;
        }

        public IActionResult DepartmentSelect() => View();

        [HttpPost]
        public IActionResult SelectDepartment(Department department)
        {
            return RedirectToAction("Index", new { dept = department });
        }

        public IActionResult Index(string category, Department? dept)
        {
            // Retrieve all items
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

        [HttpGet]
        public IActionResult Edit(string selectedCategory, string selectedSubCategory)
        {
            var extendModel = new ExtendContentModel();
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

            var viewModel = BuildEditContentViewModel(new NewContentModel(), extendModel);
            ViewBag.AllItemsJson = JsonSerializer.Serialize(items);
            return View(viewModel);
        }

        [HttpPost]
        public IActionResult EditNewContent([Bind(Prefix = "NewContent")] NewContentModel model)
        {
            var allItems = _contentService.GetContentItems();

            // Check if category already exists
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

            var vm = BuildEditContentViewModel(model, new ExtendContentModel());
            vm = AddAllItemsJson(vm);
            return View("Edit", vm);
        }

        [HttpPost]
        public IActionResult ExtendContent([Bind(Prefix = "ExtendContent")] ExtendContentModel model)
        {
            var items = _contentService.GetContentItems();
            var chosenCategory = model.SelectedCategory?.Trim();
            var chosenSubCategory = model.SelectedSubCategory?.Trim();
            var newSubCat = model.NewSubCategory?.Trim();

            // If a new subcategory is provided, use that; otherwise use chosenSubCategory.
            var actualSubCategory = !string.IsNullOrWhiteSpace(newSubCat)
                ? newSubCat
                : chosenSubCategory;

            Console.WriteLine($"ExtendContent POST => chosenCategory: {chosenCategory}, chosenSubCategory: {chosenSubCategory}, newSubCategory: {newSubCat}");

            // If neither an existing subcategory nor a new one is provided, attempt to default to the first available subcategory.
            if (string.IsNullOrWhiteSpace(chosenSubCategory) && string.IsNullOrWhiteSpace(newSubCat))
            {
                var defaultSub = items.FirstOrDefault(x =>
                    x.Category.Equals(chosenCategory, StringComparison.OrdinalIgnoreCase))?.SubCategory;

                if (!string.IsNullOrWhiteSpace(defaultSub))
                {
                    chosenSubCategory = defaultSub;
                    model.SelectedSubCategory = defaultSub;
                    actualSubCategory = defaultSub;
                    Console.WriteLine($"Defaulting chosenSubCategory to: {defaultSub}");
                }
                else
                {
                    ModelState.AddModelError("ExtendContent.SelectedSubCategory",
                        "Lütfen mevcut bir alt kategori seçiniz veya yeni bir alt kategori giriniz.");
                    Console.WriteLine("No subcategory provided and no default available.");
                }
            }

            // If a new subcategory is provided, check if it already exists.
            if (!string.IsNullOrWhiteSpace(newSubCat))
            {
                bool subCatExists = items.Any(x =>
                    x.Category.Equals(chosenCategory, StringComparison.OrdinalIgnoreCase) &&
                    x.SubCategory.Equals(actualSubCategory, StringComparison.OrdinalIgnoreCase));

                if (subCatExists)
                {
                    ModelState.AddModelError("ExtendContent.NewSubCategory", "Bu alt kategori zaten mevcut!");
                    Console.WriteLine("Duplicate new subcategory found.");
                }
            }

            // If ModelState is still invalid, show the errors.
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"ModelState Error: {key}: {error.ErrorMessage}");
                    }
                }

                var vm = BuildEditContentViewModel(new NewContentModel(), model);
                var allItemsJson = JsonSerializer.Serialize(items);
                ViewBag.AllItemsJson = allItemsJson;
                return View("Edit", vm);
            }

            // Process EditedCategory if provided.
            if (!string.IsNullOrWhiteSpace(model.EditedCategory) && model.EditedCategory.Trim() != chosenCategory)
            {
                string newCategory = model.EditedCategory.Trim();

                foreach (var item in items.Where(x =>
                         x.Category.Equals(chosenCategory, StringComparison.OrdinalIgnoreCase)))
                {
                    item.Category = newCategory;
                }

                chosenCategory = newCategory;
                Console.WriteLine($"EditedCategory processed. New category: {newCategory}");
            }

            // Process EditedSubCategory if provided.
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
                Console.WriteLine($"EditedSubCategory processed. New subcategory: {newSubCategory}");
            }

            // Now either update existing or add new content
            var existingEntry = items.FirstOrDefault(x =>
                x.Category.Equals(chosenCategory, StringComparison.OrdinalIgnoreCase) &&
                x.SubCategory.Equals(actualSubCategory, StringComparison.OrdinalIgnoreCase));

            if (existingEntry != null)
            {
                existingEntry.Content = model.Content?.Trim();
                Console.WriteLine("Existing entry updated.");
            }
            else
            {
                items.Add(new ContentItem
                {
                    Category = chosenCategory,
                    SubCategory = actualSubCategory,
                    Content = model.Content?.Trim()
                });
                Console.WriteLine("New content item added.");
            }

            _contentService.UpdateContentItems(items);
            TempData["SuccessMessage"] = "İçerik güncellendi.";
            return RedirectToAction("Edit");
        }

        [HttpPost]
        public IActionResult UpdateCategory([FromBody] UpdateCategoryViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.OldCategory) || string.IsNullOrWhiteSpace(model.NewCategory))
            {
                return Json(new { success = false, message = "Eski veya yeni kategori ismi boş olamaz." });
            }

            var items = _contentService.GetContentItems();
            var oldCat = model.OldCategory.Trim();
            var newCat = model.NewCategory.Trim();

            // Check if the newCat already exists (avoid accidental merging)
            if (items.Any(x => x.Category.Equals(newCat, StringComparison.OrdinalIgnoreCase)))
            {
                return Json(new { success = false, message = "Bu kategori zaten mevcut." });
            }

            Console.WriteLine($"UpdateCategory => Renaming '{oldCat}' to '{newCat}'");

            foreach (var item in items.Where(x => x.Category.Equals(oldCat, StringComparison.OrdinalIgnoreCase)))
            {
                item.Category = newCat;
            }

            _contentService.UpdateContentItems(items);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult UpdateSubCategory([FromBody] UpdateSubCategoryViewModel model)
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

            // Check if the new subcategory already exists
            if (items.Any(x =>
                x.Category.Equals(cat, StringComparison.OrdinalIgnoreCase) &&
                x.SubCategory.Equals(newSub, StringComparison.OrdinalIgnoreCase)))
            {
                return Json(new { success = false, message = "Bu alt kategori zaten mevcut." });
            }

            Console.WriteLine($"UpdateSubCategory => Renaming '{oldSub}' to '{newSub}' under '{cat}' category.");

            foreach (var item in items.Where(x =>
                x.Category.Equals(cat, StringComparison.OrdinalIgnoreCase) &&
                x.SubCategory.Equals(oldSub, StringComparison.OrdinalIgnoreCase)))
            {
                item.SubCategory = newSub;
            }

            _contentService.UpdateContentItems(items);
            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult GetContentItems()
        {
            var items = _contentService.GetContentItems();
            return Json(items);
        }

        public IActionResult ConvertedContent()
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

        private EditContentViewModel BuildEditContentViewModel(NewContentModel newContent, ExtendContentModel extendContent)
        {
            var allItems = _contentService.GetContentItems();

            var categories = allItems
                .Select(x => x.Category)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var subCategories = allItems
                .GroupBy(x => x.Category)
                .ToDictionary(g => g.Key,
                              g => g.Select(x => x.SubCategory)
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .OrderBy(s => s)
                                    .ToList());

            return new EditContentViewModel
            {
                ExistingCategories = categories,
                ExistingSubCategories = subCategories,
                NewContent = newContent,
                ExtendContent = extendContent
            };
        }

        private EditContentViewModel AddAllItemsJson(EditContentViewModel vm)
        {
            var allItems = _contentService.GetContentItems();
            ViewBag.AllItemsJson = JsonSerializer.Serialize(allItems);
            return vm;
        }
        [HttpPost]
        public IActionResult DeleteCategory([FromBody] DeleteCategoryViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Category))
            {
                return Json(new { success = false, message = "Kategori ismi boş olamaz." });
            }

            var items = _contentService.GetContentItems();
            var categoryToDelete = model.Category.Trim();

            // Remove all items that belong to the specified category.
            int removedCount = items.RemoveAll(x =>
                x.Category.Equals(categoryToDelete, StringComparison.OrdinalIgnoreCase));

            _contentService.UpdateContentItems(items);

            return Json(new { success = true, message = $"{removedCount} içerik silindi." });
        }

    }
}
