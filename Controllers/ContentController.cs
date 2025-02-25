using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TestKB.Models;
using TestKB.Services;
using TestKB.ViewModels;

namespace TestKB.Controllers
{
    /// <summary>
    /// İçerik işlemlerini yöneten denetleyici.
    /// </summary>
    public class ContentController : Controller
    {
        private readonly IContentManager _contentManager;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ContentController> _logger;
        private readonly IErrorHandlingService _errorHandlingService;

        /// <summary>
        /// ContentController sınıfının yapıcı metodu.
        /// </summary>
        public ContentController(
            IContentManager contentManager,
            IWebHostEnvironment env,
            ILogger<ContentController> logger,
            IErrorHandlingService errorHandlingService)
        {
            _contentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
        }

        /// <summary>
        /// Departman seçimi görünümünü döndürür.
        /// </summary>
        public IActionResult DepartmentSelect() => View();

        /// <summary>
        /// Seçilen departmana göre yönlendirme yapar.
        /// </summary>
        [HttpPost]
        public IActionResult SelectDepartment(Department department)
        {
            return RedirectToAction("Index", new { dept = department });
        }

        /// <summary>
        /// Ana sayfa içeriğini, isteğe bağlı kategori filtresiyle görüntüler.
        /// </summary>
        /// <param name="category">Filtre için kategori</param>
        public IActionResult Index(string category, Department? dept)
        {
            try
            {
                var viewModel = _contentManager.BuildContentListViewModel(category);
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

        /// <summary>
        /// Düzenleme sayfasını yükler ve gerekli view modeli oluşturur.
        /// </summary>
        [HttpGet]
        public IActionResult Edit(string selectedCategory, string selectedSubCategory)
        {
            try
            {
                var extendModel = _contentManager.CreateExtendContentViewModel(selectedCategory, selectedSubCategory);
                var viewModel = _contentManager.BuildEditContentViewModel(new NewContentViewModel(), extendModel);
                var allItems = _contentManager.GetAllContentItems();
                ViewBag.AllItemsJson = JsonSerializer.Serialize(allItems);
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

        /// <summary>
        /// Yeni içerik ekleme işlemini gerçekleştirir.
        /// </summary>
        /// <param name="model">Yeni içerik view modeli</param>
        [HttpPost]
        public IActionResult EditNewContent([Bind(Prefix = "NewContent")] NewContentViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var validationErrors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);

                    var error = _errorHandlingService.HandleValidationErrors(validationErrors);

                    var allItems = _contentManager.GetAllContentItems();
                    var viewModel = _contentManager.BuildEditContentViewModel(model, new ExtendContentViewModel());
                    ViewBag.AllItemsJson = JsonSerializer.Serialize(allItems);
                    TempData["ErrorMessage"] = error.Message;
                    TempData["ActiveTab"] = "newContent";
                    return View("Edit", viewModel);
                }

                _contentManager.AddNewContent(model);

                TempData["SuccessMessage"] = _errorHandlingService.CreateSuccessResponse("İçerik başarıyla eklendi.").Message;
                return RedirectToAction("Edit");
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Yeni içerik ekleme");

                var allItems = _contentManager.GetAllContentItems();
                var viewModel = _contentManager.BuildEditContentViewModel(model, new ExtendContentViewModel());
                ViewBag.AllItemsJson = JsonSerializer.Serialize(allItems);
                TempData["ErrorMessage"] = error.Message;
                TempData["ActiveTab"] = "newContent";
                return View("Edit", viewModel);
            }
        }

        /// <summary>
        /// Mevcut içeriği günceller.
        /// </summary>
        /// <param name="model">Genişletilmiş içerik view modeli</param>
        [HttpPost]
        public IActionResult ExtendContent([Bind(Prefix = "ExtendContent")] ExtendContentViewModel model)
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
                    var viewModel = _contentManager.BuildEditContentViewModel(new NewContentViewModel(), model);
                    var allItems = _contentManager.GetAllContentItems();
                    ViewBag.AllItemsJson = JsonSerializer.Serialize(allItems);
                    TempData["ErrorMessage"] = error.Message;
                    return View("Edit", viewModel);
                }

                _contentManager.UpdateContent(model);

                TempData["SuccessMessage"] = _errorHandlingService.CreateSuccessResponse("İçerik başarıyla güncellendi.").Message;
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
                var viewModel = _contentManager.BuildEditContentViewModel(new NewContentViewModel(), model);
                var allItems = _contentManager.GetAllContentItems();
                ViewBag.AllItemsJson = JsonSerializer.Serialize(allItems);
                return View("Edit", viewModel);
            }
        }

        /// <summary>
        /// Kategori güncelleme işlemini gerçekleştirir.
        /// </summary>
        /// <param name="model">Kategori güncelleme view modeli</param>
        [HttpPost]
        public IActionResult UpdateCategory([FromBody] UpdateCategoryViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.OldCategory) || string.IsNullOrWhiteSpace(model.NewCategory))
                {
                    return Json(_errorHandlingService.HandleValidationErrors(
                        new[] { "Eski veya yeni kategori adı boş olamaz." }));
                }

                _contentManager.UpdateCategory(model.OldCategory, model.NewCategory);

                return Json(_errorHandlingService.CreateSuccessResponse("Kategori başarıyla güncellendi."));
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Updating category");
                return Json(error);
            }
        }

        /// <summary>
        /// Alt kategori güncelleme işlemini gerçekleştirir.
        /// </summary>
        /// <param name="model">Alt kategori güncelleme view modeli</param>
        [HttpPost]
        public IActionResult UpdateSubCategory([FromBody] UpdateSubCategoryViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Category) ||
                    string.IsNullOrWhiteSpace(model.OldSubCategory) ||
                    string.IsNullOrWhiteSpace(model.NewSubCategory))
                {
                    return Json(_errorHandlingService.HandleValidationErrors(
                        new[] { "Kategori veya alt kategori bilgisi boş olamaz." }));
                }

                _contentManager.UpdateSubCategory(model.Category, model.OldSubCategory, model.NewSubCategory);

                return Json(_errorHandlingService.CreateSuccessResponse("Alt kategori başarıyla güncellendi."));
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Updating subcategory");
                return Json(error);
            }
        }

        /// <summary>
        /// Yeni alt kategori ekleme işlemini gerçekleştirir.
        /// </summary>
        /// <param name="model">Yeni alt kategori view modeli</param>
        [HttpPost]
        public IActionResult AddSubCategory([FromBody] AddSubCategoryViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Category) || string.IsNullOrWhiteSpace(model.NewSubCategory))
                {
                    return Json(_errorHandlingService.HandleValidationErrors(
                        new[] { "Kategori veya yeni alt kategori bilgisi boş olamaz." }));
                }

                _contentManager.AddSubCategory(model.Category, model.NewSubCategory);

                return Json(_errorHandlingService.CreateSuccessResponse("Alt kategori başarıyla eklendi."));
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Adding subcategory");
                return Json(error);
            }
        }

        /// <summary>
        /// Tüm içerik öğelerini JSON formatında döndürür.
        /// </summary>
        [HttpGet]
        public IActionResult GetContentItems()
        {
            try
            {
                var items = _contentManager.GetAllContentItems();
                return Json(new { success = true, data = items });
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Retrieving content items");
                return Json(error);
            }
        }

        /// <summary>
        /// JSON dosyasındaki içeriği dönüştürüp görüntüleyen sayfayı yükler.
        /// </summary>
        public IActionResult ConvertedContent()
        {
            try
            {
                string jsonFilePath = Path.Combine(_env.WebRootPath, "data.json");
                if (!System.IO.File.Exists(jsonFilePath))
                {
                    TempData["ErrorMessage"] = "JSON dosyası bulunamadı.";
                    return View((object)"");
                }

                string jsonContent = System.IO.File.ReadAllText(jsonFilePath);
                string plainText = TestKB.Services.JsonToTextConverter.ConvertJsonToText(jsonContent);

                return View((object)plainText);
            }
            catch (Exception ex)
            {
                var error = _errorHandlingService.HandleException(ex, "Converting content");

                TempData["ErrorMessage"] = error.Message;
                return View((object)"İçerik dönüştürülürken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Belirtilen kategoriye ait içerik öğelerini siler.
        /// </summary>
        /// <param name="model">Kategori silme view modeli</param>
        [HttpPost]
        public IActionResult DeleteCategory([FromBody] DeleteCategoryViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Category))
                {
                    return Json(_errorHandlingService.HandleValidationErrors(
                        new[] { "Kategori adı boş olamaz." }));
                }

                int removedCount = _contentManager.DeleteCategory(model.Category);

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