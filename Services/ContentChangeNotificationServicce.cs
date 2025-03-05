using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestKB.Services.Interfaces;

namespace TestKB.Services
{
    /// <summary>
    /// İçerik değişikliklerini Python betiğine bildiren servis implementasyonu.
    /// </summary>
    public class ContentChangeNotificationService : INotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ContentChangeNotificationService> _logger;
        private readonly string _pythonEndpointUrl;

        public ContentChangeNotificationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ContentChangeNotificationService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Konfigürasyondan Python betiği URL'sini al veya varsayılan değeri kullan
            _pythonEndpointUrl = configuration.GetValue<string>("Notification:PythonEndpoint") 
                ?? "http://localhost:5000/update";
        }

        /// <summary>
        /// İçerik değişikliğini Python betiğine bildirir.
        /// </summary>
        /// <returns>Bildirim başarıyla gönderildiyse true, aksi halde false</returns>
        public async Task<bool> NotifyContentChangeAsync()
        {
            try
            {
                _logger.LogInformation("Python betiğine bildirim gönderiliyor: {Url}", _pythonEndpointUrl);
                
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync(_pythonEndpointUrl, null);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("İçerik değişiklik bildirimi başarıyla gönderildi");
                    return true;
                }
                else
                {
                    _logger.LogWarning("İçerik değişiklik bildirimi gönderilemedi: HTTP {StatusCode}", 
                        (int)response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İçerik değişiklik bildirimi gönderilirken hata oluştu");
                return false;
            }
        }
    }
}