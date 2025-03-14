using Microsoft.AspNetCore.Mvc;
using TestKB.Services.Interfaces;

namespace TestKB.Controllers
{
    /// <summary>
    /// Python bildirim servisi ile ilgili teşhis kontrolcüsü.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationDiagnosticController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationDiagnosticController> _logger;

        public NotificationDiagnosticController(
            INotificationService notificationService,
            ILogger<NotificationDiagnosticController> logger)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Python servisinin sağlık durumunu kontrol eder.
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> CheckHealth()
        {
            _logger.LogInformation("Python servisi sağlık kontrolü başlatılıyor...");

            try
            {
                // Assuming we extend the INotificationService with this method
                if (_notificationService is Services.ContentChangeNotificationService enhancedService)
                {
                    var isAvailable = await enhancedService.CheckServiceAvailabilityAsync();

                    if (isAvailable)
                    {
                        return Ok(new { status = "success", message = "Python servisi erişilebilir durumda." });
                    }
                    else
                    {
                        return StatusCode(503, new { status = "error", message = "Python servisine erişilemiyor." });
                    }
                }
                else
                {
                    // Fallback if we're using a different notification service implementation
                    return Ok(new { status = "unknown", message = "Geliştirilmiş bildirim servisi kullanılmıyor." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sağlık kontrolü sırasında hata oluştu");
                return StatusCode(500, new { status = "error", message = $"Sağlık kontrolü sırasında hata: {ex.Message}" });
            }
        }

        /// <summary>
        /// Test amaçlı manuel bir bildirim gönderir.
        /// </summary>
        [HttpPost("test-notification")]
        public async Task<IActionResult> TestNotification()
        {
            _logger.LogInformation("Test bildirimi gönderiliyor...");

            try
            {
                var success = await _notificationService.NotifyContentChangeAsync();

                if (success)
                {
                    return Ok(new { status = "success", message = "Test bildirimi başarıyla gönderildi." });
                }
                else
                {
                    return StatusCode(503, new { status = "error", message = "Test bildirimi gönderilemedi." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test bildirimi gönderilirken hata oluştu");
                return StatusCode(500, new { status = "error", message = $"Test bildirimi gönderilirken hata: {ex.Message}" });
            }
        }
    }
}