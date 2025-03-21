using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestKB.Services.Interfaces;

namespace TestKB.Services.Notification
{
    public abstract class BaseNotificationService : INotificationService
    {
        protected readonly ILogger _logger;
        protected readonly IConfiguration _configuration;
        protected readonly string _manualUpdateUrl;
        protected readonly int _timeoutSeconds;

        public NotificationResult LastNotificationResult { get; protected set; } = new NotificationResult();
        public string LastDiagnosticResult { get; protected set; } = string.Empty;

        protected BaseNotificationService(IConfiguration configuration, ILogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Get configuration with fallbacks
            var baseUrl = _configuration["Notification:PythonEndpoint"] ??
                         "https://701b-88-230-170-83.ngrok-free.app";
            _timeoutSeconds = _configuration.GetValue<int>("Notification:TimeoutSeconds", 5);

            // Use the manual-update endpoint which is known to work
            var uri = new Uri(baseUrl);
            _manualUpdateUrl = $"{uri.Scheme}://{uri.Authority}/manual-update";

            _logger.LogInformation("{ServiceName} initialized with URL: {Url}",
                GetType().Name, _manualUpdateUrl);
        }

        // Common implementation for service availability check
        public virtual async Task<bool> CheckServiceAvailabilityAsync()
        {
            var diagnosticInfo = new StringBuilder();

            try
            {
                var uri = new Uri(_manualUpdateUrl);
                var healthCheckUrl = $"{uri.Scheme}://{uri.Authority}/health";

                diagnosticInfo.AppendLine($"Checking health at: {healthCheckUrl}");
                _logger.LogInformation("Python service health check: {Url}", healthCheckUrl);

                // Create a simple HttpClient without factory to avoid Plesk issues
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

        // Abstract method that must be implemented
        public abstract Task<bool> NotifyContentChangeAsync();

        // Common helper to get detailed error messages
        protected string GetDetailedErrorMessage(Exception ex)
        {
            if (ex is HttpRequestException httpEx)
            {
                var statusCode = httpEx.StatusCode.HasValue ? $" (Status code: {(int)httpEx.StatusCode.Value})" : "";
                return $"{httpEx.Message}{statusCode}";
            }

            if (ex is TaskCanceledException)
            {
                return "Request timed out. Either the server is not responding or the timeout is too short.";
            }

            // If there's an inner exception, include its message
            if (ex.InnerException != null)
            {
                return $"{ex.Message} → {ex.InnerException.Message}";
            }

            return ex.Message;
        }
    }
}