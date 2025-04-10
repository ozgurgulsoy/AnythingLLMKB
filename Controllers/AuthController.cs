using Microsoft.AspNetCore.Mvc;

namespace TestKB.Controllers
{
    public class AuthController : Controller
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _config;

        // Static password - in production, this should be stored securely
        private const string DEFAULT_PASSWORD = "KB2025!";

        public AuthController(ILogger<AuthController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Check if user is already authenticated
            if (HttpContext.Session.GetString("IsAuthenticated") == "true")
            {
                return RedirectToAction("DepartmentSelect", "Content");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Login(string password)
        {
            // Get password from configuration if available, otherwise use default
            string correctPassword = _config["Auth:Password"] ?? DEFAULT_PASSWORD;

            if (password == correctPassword)
            {
                // Set authentication in session
                HttpContext.Session.SetString("IsAuthenticated", "true");
                _logger.LogInformation("User successfully authenticated");

                return RedirectToAction("DepartmentSelect", "Content");
            }

            _logger.LogWarning("Failed login attempt");
            ViewBag.ErrorMessage = "Geçersiz şifre. Lütfen tekrar deneyin.";
            return View();
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("IsAuthenticated");
            return RedirectToAction("Login");
        }
    }
}