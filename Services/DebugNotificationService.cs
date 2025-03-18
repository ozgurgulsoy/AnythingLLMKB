using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestKB.Services.Interfaces;

namespace TestKB.Services
{
    public class DebugNotificationService : INotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DebugNotificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _pythonEndpointUrl;
        private readonly int _timeoutSeconds;

        // For tracking the last notification result
        public NotificationResult LastNotificationResult { get; private set; } = new NotificationResult();
        public string LastDiagnosticResult { get; private set; } = string.Empty;

        public DebugNotificationService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<DebugNotificationService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Get configuration with fallbacks
            var baseUrl = _configuration["Notification:PythonEndpoint"] ??
                          "https://e2bd-88-230-170-83.ngrok-free.app";
            var path = _configuration["Notification:EndpointPath"] ?? "update";
            _timeoutSeconds = _configuration.GetValue<int>("Notification:TimeoutSeconds", 5);

            // Combine the URL and path
            var uri = new Uri(baseUrl);
            _pythonEndpointUrl = $"{uri.Scheme}://{uri.Authority}/{path.TrimStart('/')}";

            _logger.LogInformation("DebugNotificationService initialized with URL: {Url}, Timeout: {Timeout}s",
                _pythonEndpointUrl, _timeoutSeconds);
        }

        public async Task<bool> NotifyContentChangeAsync()
        {
            var notificationResult = new NotificationResult();
            LastNotificationResult = notificationResult;

            try
            {
                _logger.LogInformation("Starting content change notification DEBUG process");
                _logger.LogInformation("Will try to notify: {Endpoint}", _pythonEndpointUrl);

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

                // Set detailed request tracing
                client.DefaultRequestHeaders.Add("User-Agent", "TestKB-Notification-Service/1.0");

                // Create notification payload with timestamp for tracking
                var payload = new
                {
                    timestamp = DateTime.UtcNow.ToString("o"),
                    source = "TestKB",
                    action = "content_updated",
                    debug = true
                };

                // Log what we're about to send
                var payloadJson = JsonSerializer.Serialize(payload);
                _logger.LogInformation("Sending payload: {Payload}", payloadJson);

                // Use POST with JSON payload for more explicit intent
                var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                try
                {
                    // Try direct HTTP call first
                    _logger.LogInformation("Sending direct HTTP POST request to {Url}", _pythonEndpointUrl);
                    var response = await client.PostAsync(_pythonEndpointUrl, content);

                    _logger.LogInformation("Received response: Status {StatusCode}", response.StatusCode);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Response content: {Content}",
                        string.IsNullOrEmpty(responseContent) ? "[Empty]" : responseContent);

                    notificationResult.StatusCode = response.StatusCode;
                    notificationResult.ResponseContent = responseContent;

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Successfully notified Python service. Response: {Response}",
                            responseContent);

                        notificationResult.Success = true;
                        notificationResult.Message = "Notification successful";
                        LastNotificationResult = notificationResult;
                        return true;
                    }

                    notificationResult.Message = $"HTTP Error: {(int)response.StatusCode} {response.ReasonPhrase}";
                    _logger.LogWarning("Failed to notify Python service: HTTP {StatusCode}. Response: {Response}",
                        (int)response.StatusCode, responseContent);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP request error notifying Python service: {Error}", ex.Message);
                    notificationResult.Exception = ex;
                    notificationResult.Message = $"Connection error: {ex.Message}";

                    if (ex.InnerException != null)
                    {
                        _logger.LogError("Inner exception: {InnerEx}", ex.InnerException.Message);
                    }
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogError(ex, "Request timeout notifying Python service: {Error}", ex.Message);
                    notificationResult.Exception = ex;
                    notificationResult.Message = "Request timed out";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error notifying Python service: {Error}", ex.Message);
                    notificationResult.Exception = ex;
                    notificationResult.Message = $"Unexpected error: {ex.Message}";
                }

                // If we get here, try using HttpClient's GetAsync as a fallback
                try
                {
                    _logger.LogInformation("First attempt failed. Trying HTTP GET as fallback to {Url}", _pythonEndpointUrl);
                    var getResponse = await client.GetAsync(_pythonEndpointUrl);

                    _logger.LogInformation("Received GET response: Status {StatusCode}", getResponse.StatusCode);
                    var getResponseContent = await getResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("GET response content: {Content}",
                        string.IsNullOrEmpty(getResponseContent) ? "[Empty]" : getResponseContent);

                    notificationResult.StatusCode = getResponse.StatusCode;
                    notificationResult.ResponseContent = getResponseContent;

                    if (getResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Successfully notified Python service via GET. Response: {Response}",
                            getResponseContent);

                        notificationResult.Success = true;
                        notificationResult.Message = "Notification successful (GET)";
                        LastNotificationResult = notificationResult;
                        return true;
                    }

                    notificationResult.Message = $"HTTP GET Error: {(int)getResponse.StatusCode} {getResponse.ReasonPhrase}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GET fallback also failed: {Error}", ex.Message);
                    // Keep the original error message from the POST attempt
                }

                // Last attempt: Try using System.Diagnostics.Process to run curl
                try
                {
                    _logger.LogInformation("Both HTTP attempts failed. Trying curl command as last resort");

                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "curl",
                            Arguments = $"-X POST \"{_pythonEndpointUrl}\" -H \"Content-Type: application/json\" -d \"{payloadJson.Replace("\"", "\\\"")}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    var stdout = await process.StandardOutput.ReadToEndAsync();
                    var stderr = await process.StandardError.ReadToEndAsync();
                    await Task.Run(() => process.WaitForExit(10000)); // Wait max 10 seconds

                    _logger.LogInformation("Curl command completed with exit code: {ExitCode}",
                        process.HasExited ? process.ExitCode : -1);
                    _logger.LogInformation("Curl stdout: {Stdout}", stdout);

                    if (!string.IsNullOrEmpty(stderr))
                    {
                        _logger.LogWarning("Curl stderr: {Stderr}", stderr);
                    }

                    if (process.HasExited && process.ExitCode == 0 && !string.IsNullOrEmpty(stdout))
                    {
                        notificationResult.Success = true;
                        notificationResult.Message = "Notification successful (curl)";
                        notificationResult.ResponseContent = stdout;
                        LastNotificationResult = notificationResult;
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Curl attempt also failed: {Error}", ex.Message);
                    // Keep the original error message
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in notification service: {Error}", ex.Message);
                notificationResult.Exception = ex;
                notificationResult.Message = $"Critical error: {ex.Message}";
            }

            LastNotificationResult = notificationResult;
            return false;
        }

        public async Task<bool> CheckServiceAvailabilityAsync()
        {
            var diagnosticInfo = new StringBuilder();

            try
            {
                var uri = new Uri(_pythonEndpointUrl);
                var healthCheckUrl = $"{uri.Scheme}://{uri.Authority}/health";

                diagnosticInfo.AppendLine($"Checking health at: {healthCheckUrl}");
                _logger.LogInformation("Python servisi sağlık kontrolü yapılıyor: {Url}", healthCheckUrl);

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

                var response = await client.GetAsync(healthCheckUrl);
                var content = await response.Content.ReadAsStringAsync();

                diagnosticInfo.AppendLine($"Status code: {(int)response.StatusCode}");
                diagnosticInfo.AppendLine($"Response: {content}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Python servisi çalışıyor: {Url}, yanıt: {Response}",
                        healthCheckUrl, content);
                    LastDiagnosticResult = diagnosticInfo.ToString();
                    return true;
                }

                _logger.LogWarning("Python servisi sağlık kontrolü başarısız: {Url}, HTTP {StatusCode}, Response: {Content}",
                    healthCheckUrl, (int)response.StatusCode, content);
            }
            catch (Exception ex)
            {
                var errorMessage = GetDetailedErrorMessage(ex);
                diagnosticInfo.AppendLine($"Error: {errorMessage}");

                _logger.LogWarning(ex, "Python servisi sağlık kontrolü başarısız: {Url}, Error: {Error}",
                    _pythonEndpointUrl, errorMessage);
            }

            LastDiagnosticResult = diagnosticInfo.ToString();
            return false;
        }

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

    // You can reuse your existing NotificationResult class
}