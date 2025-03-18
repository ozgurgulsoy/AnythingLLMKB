using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
        private readonly IConfiguration _configuration;
        private readonly string[] _pythonEndpointUrls;
        private readonly int _timeoutSeconds;
        private readonly int _retryCount;

        public ContentChangeNotificationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ContentChangeNotificationService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Get configuration values with fallbacks
            var configEndpoint = _configuration["Notification:PythonEndpoint"];
            _timeoutSeconds = _configuration.GetValue<int>("Notification:TimeoutSeconds", 5);
            _retryCount = _configuration.GetValue<int>("Notification:RetryCount", 3);

            // Try multiple endpoints in order
            _pythonEndpointUrls = !string.IsNullOrEmpty(configEndpoint)
                ? new[] { configEndpoint }
                : new[] { "https://e2bd-88-230-170-83.ngrok-free.app/update" };

            _logger.LogInformation("ContentChangeNotificationService initialized with {Count} endpoint URLs, " +
                "timeout: {Timeout}s, retries: {Retries}",
                _pythonEndpointUrls.Length, _timeoutSeconds, _retryCount);
        }

        public async Task<bool> NotifyContentChangeAsync()
        {
            _logger.LogInformation("Starting content change notification process");
            var notificationResult = new NotificationResult();

            foreach (var endpointUrl in _pythonEndpointUrls)
            {
                for (int attempt = 1; attempt <= _retryCount; attempt++)
                {
                    try
                    {
                        _logger.LogInformation("Attempting to notify Python service at {Url} (Attempt {Attempt}/{MaxAttempts})",
                            endpointUrl, attempt, _retryCount);

                        using var client = _httpClientFactory.CreateClient();
                        client.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

                        // Create notification payload with timestamp for tracking
                        var payload = new
                        {
                            timestamp = DateTime.UtcNow,
                            source = "TestKB",
                            action = "content_updated"
                        };

                        // Use POST with JSON payload for more explicit intent
                        var content = new StringContent(
                            JsonSerializer.Serialize(payload),
                            Encoding.UTF8,
                            "application/json");

                        // Send the request and capture detailed response
                        var response = await client.PostAsync(endpointUrl, content);

                        notificationResult.StatusCode = response.StatusCode;
                        notificationResult.ResponseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation("Successfully notified Python service at {Url}. Response: {Response}",
                                endpointUrl, notificationResult.ResponseContent);

                            notificationResult.Success = true;
                            notificationResult.Message = "Notification successful";

                            return true;
                        }

                        _logger.LogWarning("Failed to notify Python service at {Url}: HTTP {StatusCode}. Response: {Response}",
                            endpointUrl, (int)response.StatusCode, notificationResult.ResponseContent);

                        notificationResult.Message = $"HTTP Error: {(int)response.StatusCode} {response.ReasonPhrase}";

                        // Specific handling based on status code
                        if (response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                            response.StatusCode == HttpStatusCode.GatewayTimeout ||
                            response.StatusCode == HttpStatusCode.RequestTimeout)
                        {
                            // These are retriable errors, so we'll continue the retry loop
                            _logger.LogWarning("Retryable error encountered, will retry in 1s");
                            await Task.Delay(1000); // Wait before retry
                            continue;
                        }

                        // For other status codes, we'll break out of the retry loop and try the next endpoint
                        break;
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, "HTTP request error notifying Python service at {Url} (Attempt {Attempt}/{MaxAttempts}): {Error}",
                            endpointUrl, attempt, _retryCount, ex.Message);

                        notificationResult.Exception = ex;
                        notificationResult.Message = $"Connection error: {GetDetailedErrorMessage(ex)}";

                        // Check if we should retry based on the specific exception
                        if (ShouldRetry(ex) && attempt < _retryCount)
                        {
                            _logger.LogWarning("Retryable error encountered, will retry in 1s");
                            await Task.Delay(1000); // Wait before retry
                            continue;
                        }
                    }
                    catch (TaskCanceledException ex)
                    {
                        _logger.LogError(ex, "Timeout notifying Python service at {Url} (Attempt {Attempt}/{MaxAttempts}): {Error}",
                            endpointUrl, attempt, _retryCount, ex.Message);

                        notificationResult.Exception = ex;
                        notificationResult.Message = "Request timed out";

                        if (attempt < _retryCount)
                        {
                            _logger.LogWarning("Timeout error, will retry in 1s");
                            await Task.Delay(1000);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error notifying Python service at {Url} (Attempt {Attempt}/{MaxAttempts}): {Error}",
                            endpointUrl, attempt, _retryCount, ex.Message);

                        notificationResult.Exception = ex;
                        notificationResult.Message = $"Unexpected error: {ex.Message}";

                        // Non-HTTP exceptions are generally not retriable
                        break;
                    }
                }
            }

            // Store the last notification result for diagnostic purposes
            LastNotificationResult = notificationResult;

            _logger.LogError("All notification attempts failed. Last error: {Error}", notificationResult.Message);
            return false;
        }

        /// <summary>
        /// Contains detailed information about the last notification attempt
        /// </summary>
        public NotificationResult LastNotificationResult { get; private set; } = new NotificationResult();

        /// <summary>
        /// Python servisinin çalışıp çalışmadığını kontrol eder.
        /// </summary>
        public async Task<bool> CheckServiceAvailabilityAsync()
        {
            var diagnosticInfo = new List<string>();

            foreach (var baseUrl in _pythonEndpointUrls)
            {
                try
                {
                    // Extract the base URL without the path
                    var uri = new Uri(baseUrl);
                    var healthCheckUrl = $"{uri.Scheme}://{uri.Authority}/health";

                    diagnosticInfo.Add($"Checking health at: {healthCheckUrl}");
                    _logger.LogInformation("Python servisi sağlık kontrolü yapılıyor: {Url}", healthCheckUrl);

                    using var client = _httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

                    var response = await client.GetAsync(healthCheckUrl);
                    var content = await response.Content.ReadAsStringAsync();

                    diagnosticInfo.Add($"Status code: {(int)response.StatusCode}");
                    diagnosticInfo.Add($"Response: {content}");

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Python servisi çalışıyor: {Url}, yanıt: {Response}",
                            healthCheckUrl, content);

                        LastDiagnosticResult = string.Join("\n", diagnosticInfo);
                        return true;
                    }

                    _logger.LogWarning("Python servisi sağlık kontrolü başarısız: {Url}, HTTP {StatusCode}, Response: {Content}",
                        healthCheckUrl, (int)response.StatusCode, content);
                }
                catch (Exception ex)
                {
                    var errorMessage = GetDetailedErrorMessage(ex);
                    diagnosticInfo.Add($"Error: {errorMessage}");

                    _logger.LogWarning(ex, "Python servisi sağlık kontrolü başarısız: {Url}, Error: {Error}",
                        baseUrl, errorMessage);
                }
            }

            LastDiagnosticResult = string.Join("\n", diagnosticInfo);
            _logger.LogError("Python servisi erişilemez. Hiçbir endpoint yanıt vermedi. Diagnostic: {Diagnostic}",
                LastDiagnosticResult);

            return false;
        }

        /// <summary>
        /// Contains diagnostic information from the last health check
        /// </summary>
        public string LastDiagnosticResult { get; private set; } = string.Empty;

        /// <summary>
        /// Determine if an exception should trigger a retry
        /// </summary>
        private bool ShouldRetry(HttpRequestException ex)
        {
            // Check for specific status codes in the HttpRequestException
            if (ex.StatusCode.HasValue)
            {
                var statusCode = (int)ex.StatusCode.Value;
                return statusCode >= 500 || statusCode == 429; // Server errors or rate limiting
            }

            // Check the message for common network issues
            var message = ex.Message.ToLowerInvariant();
            return message.Contains("timeout") ||
                   message.Contains("connection refused") ||
                   message.Contains("no such host") ||
                   message.Contains("network") ||
                   message.Contains("socket");
        }

        /// <summary>
        /// Get a detailed error message with network information
        /// </summary>
        private string GetDetailedErrorMessage(Exception ex)
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

    /// <summary>
    /// Contains detailed information about a notification attempt
    /// </summary>
    public class NotificationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public HttpStatusCode? StatusCode { get; set; }
        public string ResponseContent { get; set; } = string.Empty;
        public Exception? Exception { get; set; }

        public string GetDetailedReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Success: {Success}");
            sb.AppendLine($"Message: {Message}");

            if (StatusCode.HasValue)
            {
                sb.AppendLine($"Status code: {(int)StatusCode.Value} ({StatusCode})");
            }

            if (!string.IsNullOrEmpty(ResponseContent))
            {
                sb.AppendLine($"Response: {ResponseContent}");
            }

            if (Exception != null)
            {
                sb.AppendLine($"Exception type: {Exception.GetType().Name}");
                sb.AppendLine($"Exception message: {Exception.Message}");

                if (Exception.InnerException != null)
                {
                    sb.AppendLine($"Inner exception: {Exception.InnerException.Message}");
                }
            }

            return sb.ToString();
        }
    }
}