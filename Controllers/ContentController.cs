
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TestKB.Models;
using TestKB.Models.ViewModels;
using TestKB.Services.Interfaces;

namespace TestKB.Controllers
{
    public class ContentController(
        IContentManager contentManager,
        IWebHostEnvironment env,
        ILogger<ContentController> logger,
        IErrorHandlingService errorHandlingService,
        IContentService contentService)
        : Controller
    {
        private readonly IContentManager _contentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
        private readonly IWebHostEnvironment _env = env ?? throw new ArgumentNullException(nameof(env));
        private readonly ILogger<ContentController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IErrorHandlingService _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
        private readonly IContentService _contentService = contentService ?? throw new ArgumentNullException(nameof(contentService));
        
        public IActionResult DepartmentSelect() => View();
        
        [HttpPost]
        public IActionResult SelectDepartment(Department department)
        {
            return RedirectToAction("Index", new { dept = department });
        }

        public async Task<IActionResult> Index(string category, Department? dept)
        {
            try
            {
                var viewModel = await _contentManager.BuildContentListViewModelAsync(category);
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
                
                // Get fresh data
                var freshItems = await _contentService.GetAllAsync(true);
                
                // Create the view model
                var extendModel = await _contentManager.CreateExtendContentViewModelAsync(
                    selectedCategory, selectedSubCategory);
                
                var viewModel = await _contentManager.BuildEditContentViewModelAsync(
                    new NewContentViewModel(), extendModel);
                
                // Add timestamp to see the exact time this was generated
                ViewBag.LastRefreshedTime = DateTime.Now.ToString("HH:mm:ss.fff");
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

                await _contentManager.UpdateContentAsync(model);

                TempData["SuccessMessage"] = _errorHandlingService.CreateSuccessResponse("İçerik başarıyla güncellendi.").Message;
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

        public async Task<IActionResult> ConvertedContent()
        {
            try
            {
                var jsonFilePath = Path.Combine(_env.WebRootPath, "data.json");
                if (!System.IO.File.Exists(jsonFilePath))
                {
                    TempData["ErrorMessage"] = "JSON dosyası bulunamadı.";
                    return View((object)"");
                }

                var jsonContent = await System.IO.File.ReadAllTextAsync(jsonFilePath);
                var plainText = Services.JsonToTextConverter.ConvertJsonToText(jsonContent);

                return View((object)plainText);
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Converting content");

                TempData["ErrorMessage"] = error.Message;
                return View((object)"İçerik dönüştürülürken bir hata oluştu.");
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
    }
}