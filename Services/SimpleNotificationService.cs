using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestKB.Services.Interfaces;

namespace TestKB.Services
{
    public class SimpleNotificationService : INotificationService
    {
        private readonly ILogger<SimpleNotificationService> _logger;
        private readonly string _manualUpdateUrl;
        private readonly int _timeoutSeconds;

        public NotificationResult LastNotificationResult { get; private set; } = new NotificationResult();
        public string LastDiagnosticResult { get; private set; } = string.Empty;

        public SimpleNotificationService(
            IConfiguration configuration,
            ILogger<SimpleNotificationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Get configuration
            var baseUrl = configuration["Notification:PythonEndpoint"] ??
                         "https://e2bd-88-230-170-83.ngrok-free.app";
            _timeoutSeconds = configuration.GetValue<int>("Notification:TimeoutSeconds", 5);

            // Use the manual-update endpoint which is known to work
            var uri = new Uri(baseUrl);
            _manualUpdateUrl = $"{uri.Scheme}://{uri.Authority}/manual-update";

            _logger.LogInformation("SimpleNotificationService initialized with URL: {Url}", _manualUpdateUrl);
        }

        public async Task<bool> NotifyContentChangeAsync()
        {
            var notificationResult = new NotificationResult();

            try
            {
                _logger.LogInformation("Sending notification using manual-update endpoint: {Url}", _manualUpdateUrl);

                // Create an HttpClient without any fancy configuration
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(_timeoutSeconds)
                };

                // Send a simple GET request to the manual-update endpoint
                var response = await client.GetAsync(_manualUpdateUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Response status: {Status}", response.StatusCode);
                _logger.LogInformation("Response content: {Content}", responseContent);

                notificationResult.StatusCode = response.StatusCode;
                notificationResult.ResponseContent = responseContent;

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully triggered manual update");
                    notificationResult.Success = true;
                    notificationResult.Message = "Manual update triggered successfully";
                    LastNotificationResult = notificationResult;
                    return true;
                }

                notificationResult.Message = $"HTTP Error: {(int)response.StatusCode} {response.ReasonPhrase}";
                _logger.LogWarning("Failed to trigger manual update: {Status}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification: {Error}", ex.Message);
                notificationResult.Exception = ex;
                notificationResult.Message = $"Error: {ex.Message}";

                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerError}", ex.InnerException.Message);
                }
            }

            LastNotificationResult = notificationResult;
            return false;
        }

        public async Task<bool> CheckServiceAvailabilityAsync()
        {
            var diagnosticInfo = new StringBuilder();

            try
            {
                var uri = new Uri(_manualUpdateUrl);
                var healthCheckUrl = $"{uri.Scheme}://{uri.Authority}/health";

                diagnosticInfo.AppendLine($"Checking health at: {healthCheckUrl}");
                _logger.LogInformation("Python service health check: {Url}", healthCheckUrl);

                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(_timeoutSeconds)
                };

                var response = await client.GetAsync(healthCheckUrl);
                var content = await response.Content.ReadAsStringAsync();

                diagnosticInfo.AppendLine($"Status code: {(int)response.StatusCode}");
                diagnosticInfo.AppendLine($"Response: {content}");

                LastDiagnosticResult = diagnosticInfo.ToString();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Python service is running");
                    return true;
                }

                _logger.LogWarning("Python service health check failed: {Status}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                diagnosticInfo.AppendLine($"Error: {ex.Message}");

                if (ex.InnerException != null)
                {
                    diagnosticInfo.AppendLine($"Inner error: {ex.InnerException.Message}");
                }

                LastDiagnosticResult = diagnosticInfo.ToString();
                _logger.LogError(ex, "Error checking service health: {Error}", ex.Message);
                return false;
            }
        }
    }
}