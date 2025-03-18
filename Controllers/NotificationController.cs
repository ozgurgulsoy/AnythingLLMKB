using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TestKB.Models.ViewModels;
using TestKB.Services.Interfaces;

namespace TestKB.Controllers
{
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;
        private readonly IConfiguration _configuration;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger,
            IConfiguration configuration)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Bildirim servisi durumunu gösteren sayfa
        /// </summary>
        public async Task<IActionResult> Status()
        {
            try
            {
                _logger.LogInformation("Bildirim durumu sayfası görüntüleniyor");

                // Check if service is available
                var isAvailable = await _notificationService.CheckServiceAvailabilityAsync();

                // Create view model
                var model = new NotificationStatusViewModel
                {
                    IsServiceAvailable = isAvailable,
                    LastNotificationSuccessful = _notificationService.LastNotificationResult?.Success ?? false,
                    EndpointUrl = _configuration["Notification:PythonEndpoint"] ?? "Yapılandırılmamış",
                    TimeoutSeconds = _configuration.GetValue<int>("Notification:TimeoutSeconds", 5),
                    RetryCount = _configuration.GetValue<int>("Notification:RetryCount", 3),
                    LastDiagnosticResult = _notificationService.LastDiagnosticResult,
                    LastNotificationDetails = _notificationService.LastNotificationResult?.GetDetailedReport() ?? string.Empty
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bildirim durumu sayfası yüklenirken hata");

                TempData["ErrorMessage"] = $"Durum bilgisi alınırken hata oluştu: {ex.Message}";

                return View(new NotificationStatusViewModel
                {
                    EndpointUrl = _configuration["Notification:PythonEndpoint"] ?? "Yapılandırılmamış",
                    TimeoutSeconds = _configuration.GetValue<int>("Notification:TimeoutSeconds", 5),
                    RetryCount = _configuration.GetValue<int>("Notification:RetryCount", 3)
                });
            }
        }

        /// <summary>
        /// Test bildirimi gönderir ve sonucu görüntüler
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestNotification()
        {
            try
            {
                _logger.LogInformation("Test bildirimi gönderiliyor");

                var success = await _notificationService.NotifyContentChangeAsync();

                if (success)
                {
                    TempData["SuccessMessage"] = "Test bildirimi başarıyla gönderildi";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Test bildirimi gönderilemedi: {_notificationService.LastNotificationResult?.Message}";
                }

                return RedirectToAction(nameof(Status));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test bildirimi gönderilirken hata");

                TempData["ErrorMessage"] = $"Test bildirimi gönderilirken hata oluştu: {ex.Message}";

                return RedirectToAction(nameof(Status));
            }
        }
    }
}