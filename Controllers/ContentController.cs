﻿using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TestKB.Models;
using TestKB.Models.ViewModels;
using TestKB.Services.Interfaces;
using TestKB.Extensions;
using MSSession = Microsoft.AspNetCore.Http.SessionExtensions;
using System.Text;
namespace TestKB.Controllers
{
    public class ContentController : Controller
    {
        private readonly IContentManager _contentManager;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ContentController> _logger;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly IContentService _contentService;

        public ContentController(
            IContentManager contentManager,
            IWebHostEnvironment env,
            ILogger<ContentController> logger,
            IErrorHandlingService errorHandlingService,
            IContentService contentService)
        {
            _contentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
        }

        public IActionResult DepartmentSelect() => View();

        [HttpPost]
        public IActionResult SelectDepartment(Department department)
        {
            // Store selected department in session using fully qualified method
            MSSession.SetInt32(HttpContext.Session, "SelectedDepartment", (int)department);
            _logger.LogInformation("Departman seçildi: {Department}", department);
            return RedirectToAction("Index");
        }

        // Helper method to get current department from session
        private Department GetCurrentDepartment()
        {
            var deptValue = MSSession.GetInt32(HttpContext.Session, "SelectedDepartment");
            if (!deptValue.HasValue)
            {
                // Default to first department if none selected
                var defaultDept = Department.Yazılım;
                MSSession.SetInt32(HttpContext.Session, "SelectedDepartment", (int)defaultDept);
                return defaultDept;
            }
            return (Department)deptValue.Value;
        }

        public async Task<IActionResult> Index(string category)
        {
            try
            {
                // Get current department from session
                Department currentDepartment = GetCurrentDepartment();

                var viewModel = await _contentManager.BuildContentListViewModelAsync(category, currentDepartment);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Index action");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(error);
                }

                TempData["ErrorMessage"] = error.Message;
                return View(new ContentListViewModel());
            }
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Duration = 0)]
        public async Task<IActionResult> Edit(string selectedCategory, string selectedSubCategory)
        {
            try
            {
                _logger.LogInformation("Edit sayfası başlatılıyor. Category: {Category}, SubCategory: {SubCategory}",
                    selectedCategory, selectedSubCategory);

                // Get current department from session
                Department department = GetCurrentDepartment();
                _logger.LogInformation("Current department: {Department}", department);

                // Get fresh data
                var freshItems = await _contentService.GetAllAsync(true);

                // Create the view model
                var extendModel = await _contentManager.CreateExtendContentViewModelAsync(
                    selectedCategory, selectedSubCategory, department);

                // Check if we have content
                _logger.LogInformation("Content retrieved: {ContentLength}",
                    extendModel.Content?.Length ?? 0);

                // Set the current department in the new content model
                var newContentModel = new NewContentViewModel { Department = department };

                var viewModel = await _contentManager.BuildEditContentViewModelAsync(
                    newContentModel, extendModel);

                // Add timestamp to see the exact time this was generated
                ViewBag.LastRefreshedTime = DateTime.Now.ToString("HH:mm:ss.fff");

                // Make sure we're properly setting the department in the model
                viewModel.SelectedDepartment = department;

                // Serialize items with the current department for client-side use
                ViewBag.AllItemsJson = JsonSerializer.Serialize(freshItems);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Edit page loading");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(error);
                }

                TempData["ErrorMessage"] = error.Message;
                return View(new EditContentViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNewContent([Bind(Prefix = "NewContent")] NewContentViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var validationErrors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);

                    var error = _errorHandlingService.HandleValidationErrors(validationErrors);

                    var allItems = await _contentService.GetAllAsync(true);
                    var viewModel = await _contentManager.BuildEditContentViewModelAsync(model, new ExtendContentViewModel());
                    ViewBag.AllItemsJson = JsonSerializer.Serialize(allItems);
                    TempData["ErrorMessage"] = error.Message;
                    TempData["ActiveTab"] = "newContent";
                    return View("Edit", viewModel);
                }

                // Ensure department is set if not provided
                if (model.Department == 0)
                {
                    model.Department = GetCurrentDepartment();
                }

                await _contentManager.AddNewContentAsync(model);

                TempData["SuccessMessage"] = _errorHandlingService.CreateSuccessResponse("İçerik başarıyla eklendi.").Message;
                return RedirectToAction("Edit");
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Yeni içerik ekleme");

                var allItems = await _contentService.GetAllAsync(true);
                var viewModel = await _contentManager.BuildEditContentViewModelAsync(model, new ExtendContentViewModel());
                ViewBag.AllItemsJson = JsonSerializer.Serialize(allItems);
                TempData["ErrorMessage"] = error.Message;
                TempData["ActiveTab"] = "newContent";
                return View("Edit", viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendContent([Bind(Prefix = "ExtendContent")] ExtendContentViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Content))
                {
                    ModelState.AddModelError("ExtendContent.Content", "İçerik boş olamaz.");
                }

                if (!ModelState.IsValid)
                {
                    var validationErrors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);

                    var error = _errorHandlingService.HandleValidationErrors(validationErrors);

                    _logger.LogWarning("ModelState hataları: {Errors}", string.Join("; ", validationErrors));

                    var freshItems = await _contentService.GetAllAsync(true);
                    var viewModel = await _contentManager.BuildEditContentViewModelAsync(new NewContentViewModel(), model);
                    ViewBag.AllItemsJson = JsonSerializer.Serialize(freshItems);
                    TempData["ErrorMessage"] = error.Message;
                    TempData["ActiveTab"] = "extendContent";
                    return View("Edit", viewModel);
                }

                // Instead of a single content item, we need to update all matching category/subcategory items
                var allItems = await _contentService.GetAllAsync(true);
                var matchingItems = allItems.Where(i =>
                    string.Equals(i.Category.Trim(), model.SelectedCategory.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(i.SubCategory?.Trim(), model.SelectedSubCategory?.Trim(), StringComparison.OrdinalIgnoreCase)
                ).ToList();

                if (matchingItems.Any())
                {
                    // Update all matching items with the new content
                    foreach (var item in matchingItems)
                    {
                        item.Content = model.Content;
                    }

                    await _contentService.UpdateManyAsync(allItems);

                    TempData["SuccessMessage"] = "İçerik başarıyla güncellendi.";
                }
                else
                {
                    // If no matching items exist, create a new one with the current department
                    var currentDepartment = GetCurrentDepartment();
                    var newItem = new ContentItem
                    {
                        Category = model.SelectedCategory.Trim(),
                        SubCategory = model.SelectedSubCategory?.Trim(),
                        Content = model.Content.Trim(),
                        Department = currentDepartment
                    };

                    await _contentService.CreateAsync(newItem);

                    TempData["SuccessMessage"] = "Yeni içerik başarıyla oluşturuldu.";
                }

                TempData["ActiveTab"] = "extendContent";
                return RedirectToAction("Edit");
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Updating content");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(error);
                }

                TempData["ErrorMessage"] = error.Message;

                var freshItems = await _contentService.GetAllAsync(true);
                var viewModel = await _contentManager.BuildEditContentViewModelAsync(new NewContentViewModel(), model);
                ViewBag.AllItemsJson = JsonSerializer.Serialize(freshItems);
                TempData["ActiveTab"] = "extendContent";
                return View("Edit", viewModel);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCategory([FromBody] UpdateCategoryViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.OldCategory) || string.IsNullOrWhiteSpace(model.NewCategory))
                {
                    return Json(_errorHandlingService.HandleValidationErrors(
                        ["Eski veya yeni kategori adı boş olamaz."]));
                }

                await _contentManager.UpdateCategoryAsync(model.OldCategory, model.NewCategory);

                return Json(_errorHandlingService.CreateSuccessResponse("Kategori başarıyla güncellendi."));
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Updating category");
                return Json(error);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSubCategory([FromBody] UpdateSubCategoryViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Category) ||
                    string.IsNullOrWhiteSpace(model.OldSubCategory) ||
                    string.IsNullOrWhiteSpace(model.NewSubCategory))
                {
                    return Json(_errorHandlingService.HandleValidationErrors(
                        ["Kategori veya alt kategori bilgisi boş olamaz."]));
                }

                await _contentManager.UpdateSubCategoryAsync(model.Category, model.OldSubCategory, model.NewSubCategory);

                return Json(_errorHandlingService.CreateSuccessResponse("Alt kategori başarıyla güncellendi."));
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Updating subcategory");
                return Json(error);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubCategory([FromBody] AddSubCategoryViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Category) || string.IsNullOrWhiteSpace(model.NewSubCategory))
                {
                    return Json(_errorHandlingService.HandleValidationErrors(
                        ["Kategori veya yeni alt kategori bilgisi boş olamaz."]));
                }

                await _contentManager.AddSubCategoryAsync(model.Category, model.NewSubCategory);

                return Json(_errorHandlingService.CreateSuccessResponse("Alt kategori başarıyla eklendi."));
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Adding subcategory");
                return Json(error);
            }
        }

        [HttpGet]
        [ResponseCache(Duration = 0, NoStore = true)]
        public async Task<IActionResult> GetContentItems()
        {
            try
            {
                var items = await _contentService.GetAllAsync(true);
                return Json(new { success = true, data = items });
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Retrieving content items");
                return Json(error);
            }
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> ConvertedContent()
        {
            try
            {
                var jsonFilePath = Path.Combine(_env.WebRootPath, "data.json");
                if (!System.IO.File.Exists(jsonFilePath))
                {
                    return Content("JSON dosyası bulunamadı.", "text/plain", Encoding.UTF8);
                }

                // Explicitly read with UTF-8 encoding
                var jsonContent = await System.IO.File.ReadAllTextAsync(jsonFilePath, Encoding.UTF8);
                var plainText = Services.JsonToTextConverter.ConvertJsonToText(jsonContent);

                // Return with explicit UTF-8 encoding
                return Content(plainText, "text/plain; charset=utf-8", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Converting content");
                return Content("İçerik dönüştürülürken bir hata oluştu: " + error.Message,
                              "text/plain; charset=utf-8", Encoding.UTF8);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory([FromBody] DeleteCategoryViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Category))
                {
                    return Json(_errorHandlingService.HandleValidationErrors(
                        ["Kategori adı boş olamaz."]));
                }

                var removedCount = await _contentManager.DeleteCategoryAsync(model.Category);

                return Json(_errorHandlingService.CreateSuccessResponse($"{removedCount} içerik öğesi silindi."));
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Deleting category");
                return Json(error);
            }
        }
        // Add this method to your ContentController class

        [HttpGet]
        [Route("GetContentByCategoryAndSubcategory")]
        public async Task<IActionResult> GetContentByCategoryAndSubcategory(string category, string subcategory)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(subcategory))
                {
                    return Json(new { success = false, message = "Kategori veya alt kategori boş olamaz." });
                }

                // Get content without department filtering
                var contentItem = await _contentService.GetByCategoryAndSubcategoryAsync(category, subcategory);

                if (contentItem == null)
                {
                    return Json(new { success = false, message = "İçerik bulunamadı." });
                }

                return Json(new
                {
                    success = true,
                    content = contentItem.Content,
                    category = contentItem.Category,
                    subcategory = contentItem.SubCategory,
                    department = contentItem.Department
                });
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "GetContentByCategoryAndSubcategory");
                return Json(error);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContent(string category, string subcategory, int department)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(subcategory))
                {
                    return Json(_errorHandlingService.HandleValidationErrors(
                        ["Kategori veya alt kategori bilgisi boş olamaz."]));
                }

                // Convert from int to Department enum
                Department dept = (Department)department;

                // Get the content item first
                var contentItem = await _contentService.GetByCategoryAndSubcategoryAsync(category, subcategory, dept);

                if (contentItem == null)
                {
                    return Json(_errorHandlingService.HandleValidationErrors(
                        ["İçerik bulunamadı."]));
                }

                // Instead of deleting, just clear the content but preserve structure
                contentItem.Content = string.Empty;

                // Update the item with empty content
                await _contentService.UpdateAsync(contentItem);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(_errorHandlingService.CreateSuccessResponse("İçerik başarıyla temizlendi."));
                }

                TempData["SuccessMessage"] = "İçerik başarıyla temizlendi.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Deleting content");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(error);
                }

                TempData["ErrorMessage"] = error.Message;
                return RedirectToAction("Index");
            }
        }
    }
}