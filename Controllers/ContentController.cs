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

        /// <summary>
        /// ContentController sınıfının yapıcı metodu.
        /// </summary>
        public ContentController(IContentManager contentManager, IWebHostEnvironment env, ILogger<ContentController> logger)
        {
            _contentManager = contentManager ?? throw new ArgumentNullException(nameof(contentManager));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                _logger.LogError(ex, "Error in Index action");
                return StatusCode(500, "An error occurred.");
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
                _logger.LogError(ex, "Error loading Edit page");
                return StatusCode(500, "An error occurred.");
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
                _contentManager.AddNewContent(model);
                TempData["SuccessMessage"] = "Content added successfully.";
                return RedirectToAction("Edit");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding new content");
                var allItems = _contentManager.GetAllContentItems();
                var viewModel = _contentManager.BuildEditContentViewModel(model, new ExtendContentViewModel());
                ViewBag.AllItemsJson = JsonSerializer.Serialize(allItems);
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
                    ModelState.AddModelError("ExtendContent.Content", "Content cannot be empty.");
                }
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState errors: {Errors}",
                        string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    var viewModel = _contentManager.BuildEditContentViewModel(new NewContentViewModel(), model);
                    var allItems = _contentManager.GetAllContentItems();
                    ViewBag.AllItemsJson = JsonSerializer.Serialize(allItems);
                    return View("Edit", viewModel);
                }

                _contentManager.UpdateContent(model);
                TempData["SuccessMessage"] = "Content updated successfully.";
                return RedirectToAction("Edit");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating content");
                return StatusCode(500, "An error occurred.");
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
                    return Json(new { success = false, message = "Old or new category name cannot be empty." });
                }
                _contentManager.UpdateCategory(model.OldCategory, model.NewCategory);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                return Json(new { success = false, message = "An error occurred while updating the category." });
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
                    return Json(new { success = false, message = "Category or subcategory information cannot be empty." });
                }
                _contentManager.UpdateSubCategory(model.Category, model.OldSubCategory, model.NewSubCategory);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subcategory");
                return Json(new { success = false, message = "An error occurred while updating the subcategory." });
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
                    return Json(new { success = false, message = "Category or new subcategory information cannot be empty." });
                }
                _contentManager.AddSubCategory(model.Category, model.NewSubCategory);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding subcategory");
                return Json(new { success = false, message = "An error occurred while adding the subcategory." });
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
                return Json(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving content items");
                return StatusCode(500, "An error occurred.");
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
                    return Content("JSON file not found.");
                }
                string jsonContent = System.IO.File.ReadAllText(jsonFilePath);
                string plainText = TestKB.Services.JsonToTextConverter.ConvertJsonToText(jsonContent);
                return View((object)plainText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting content");
                return StatusCode(500, "An error occurred.");
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
                    return Json(new { success = false, message = "Category name cannot be empty." });
                }
                int removedCount = _contentManager.DeleteCategory(model.Category);
                return Json(new { success = true, message = $"{removedCount} content items deleted." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                return Json(new { success = false, message = "An error occurred while deleting the category." });
            }
        }
    }
}
