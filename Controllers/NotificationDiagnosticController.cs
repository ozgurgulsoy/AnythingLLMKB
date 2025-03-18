using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using TestKB.Services.Interfaces;

namespace TestKB.Controllers
{
    [Route("api/notification-diagnostics")]
    [ApiController]
    public class NotificationDiagnosticController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationDiagnosticController> _logger;
        private readonly IConfiguration _configuration;

        public NotificationDiagnosticController(
            INotificationService notificationService,
            ILogger<NotificationDiagnosticController> logger,
            IConfiguration configuration)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Notification servisinin durumunu kontrol eder ve ayrıntılı rapor verir.
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetNotificationStatus()
        {
            _logger.LogInformation("Notification servisi tanılama başlatılıyor");

            try
            {
                // Step 1: Check if Python service is available
                var isServiceAvailable = await _notificationService.CheckServiceAvailabilityAsync();

                // Step 2: Try a test notification
                var isNotificationSuccessful = await _notificationService.NotifyContentChangeAsync();

                // Step 3: Collect network diagnostics for common endpoints
                var networkDiagnostics = await CollectNetworkDiagnosticsAsync();

                // Step 4: Prepare response
                var detailedReport = new
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    configuredEndpoints = _configuration["Notification:PythonEndpoint"],
                    serviceAvailable = isServiceAvailable,
                    notificationSuccessful = isNotificationSuccessful,
                    healthCheckDetails = _notificationService.LastDiagnosticResult,
                    notificationDetails = _notificationService.LastNotificationResult.GetDetailedReport(),
                    networkDiagnostics = networkDiagnostics
                };

                // Return detailed status
                return Ok(detailedReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tanılama sırasında beklenmeyen hata");
                return StatusCode(500, new { error = $"Tanılama hatası: {ex.Message}" });
            }
        }

        /// <summary>
        /// Test bildirimini manuel olarak tetikler ve sonucu döndürür.
        /// </summary>
        [HttpPost("send-test")]
        public async Task<IActionResult> SendTestNotification()
        {
            _logger.LogInformation("Test bildirimi gönderiliyor");

            try
            {
                var success = await _notificationService.NotifyContentChangeAsync();

                var result = new
                {
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    success = success,
                    details = _notificationService.LastNotificationResult.GetDetailedReport()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test bildirimi gönderilirken hata");
                return StatusCode(500, new { error = $"Bildirim hatası: {ex.Message}" });
            }
        }

        /// <summary>
        /// Ağ bağlantısı tanılama bilgilerini toplar
        /// </summary>
        private async Task<string> CollectNetworkDiagnosticsAsync()
        {
            var sb = new StringBuilder();

            try
            {
                // Extract domain from configured endpoint
                var endpoint = _configuration["Notification:PythonEndpoint"];
                if (string.IsNullOrEmpty(endpoint))
                {
                    endpoint = "https://5488-212-58-13-88.ngrok-free.app"; // Fallback
                }

                var uri = new Uri(endpoint);
                var host = uri.Host;

                sb.AppendLine($"Network diagnostics for {host}:");

                // Try ping
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(host, 2000);
                    sb.AppendLine($"Ping response: {reply.Status}, Time: {reply.RoundtripTime}ms");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Ping failed: {ex.Message}");
                }

                // Try traceroute-like hops (simplified)
                try
                {
                    for (int ttl = 1; ttl <= 10; ttl++)
                    {
                        using var pingTtl = new Ping();
                        var options = new PingOptions(ttl, true);
                        var buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

                        var reply = await pingTtl.SendPingAsync(host, 1000, buffer, options);

                        if (reply.Status == IPStatus.TimedOut)
                        {
                            sb.AppendLine($"Hop {ttl}: *");
                        }
                        else if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.Success)
                        {
                            sb.AppendLine($"Hop {ttl}: {reply.Address} ({reply.RoundtripTime}ms)");

                            if (reply.Status == IPStatus.Success)
                                break;
                        }
                        else
                        {
                            sb.AppendLine($"Hop {ttl}: {reply.Status}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Traceroute failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Network diagnostics failed: {ex.Message}");
            }

            return sb.ToString();
        }
    }
}