// Update existing ContentChangeNotificationService.cs
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestKB.Services.Interfaces;

namespace TestKB.Services
{
    public class ContentChangeNotificationService : INotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ContentChangeNotificationService> _logger;
        private readonly string[] _pythonEndpointUrls;

        public ContentChangeNotificationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ContentChangeNotificationService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Try multiple endpoints in order
            _pythonEndpointUrls = new[]
            {
                "http://localhost:5000/update",              // Local Python service
                "http://127.0.0.1:5000/update",              // Alternative local address
                "http://10.212.136.4:5000/update",           // IP address from your logs
            };

            _logger.LogInformation("ContentChangeNotificationService initialized with {Count} endpoint URLs",
                _pythonEndpointUrls.Length);
        }

        public async Task<bool> NotifyContentChangeAsync()
        {
            _logger.LogInformation("Starting content change notification process");

            foreach (var endpointUrl in _pythonEndpointUrls)
            {
                try
                {
                    _logger.LogInformation("Attempting to notify Python service at {Url}", endpointUrl);

                    using var client = _httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(5); // Short timeout

                    // Attempt GET request (simpler than POST for testing)
                    var response = await client.GetAsync(endpointUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Successfully notified Python service at {Url}", endpointUrl);
                        return true;
                    }

                    _logger.LogWarning("Failed to notify Python service: {StatusCode}", response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error notifying Python service at {Url}", endpointUrl);
                    // Continue trying other endpoints
                }
            }

            _logger.LogError("All notification attempts failed");
            return false;
        }
        // Add this method to your ContentChangeNotificationService.cs file

        /// <summary>
        /// Python servisinin çalışıp çalışmadığını kontrol eder.
        /// </summary>
        /// <returns>Servis çalışıyorsa true, aksi halde false</returns>
        public async Task<bool> CheckServiceAvailabilityAsync()
        {
            foreach (var baseUrl in _pythonEndpointUrls)
            {
                try
                {
                    // Extract the base URL without the path
                    var uri = new Uri(baseUrl);
                    var healthCheckUrl = $"{uri.Scheme}://{uri.Authority}/health";

                    _logger.LogInformation("Python servisi sağlık kontrolü yapılıyor: {Url}", healthCheckUrl);

                    using var client = _httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(5);

                    var response = await client.GetAsync(healthCheckUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation("Python servisi çalışıyor: {Url}, yanıt: {Response}", healthCheckUrl, content);
                        return true;
                    }

                    _logger.LogWarning("Python servisi sağlık kontrolü başarısız: {Url}, HTTP {StatusCode}",
                        healthCheckUrl,
                        (int)response.StatusCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Python servisi sağlık kontrolü başarısız: {Url}", baseUrl);
                }
            }

            _logger.LogError("Python servisi erişilemez. Hiçbir endpoint yanıt vermedi.");
            return false;
        }
    }
}