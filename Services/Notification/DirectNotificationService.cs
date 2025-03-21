using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TestKB.Services.Notification
{
    public class DirectNotificationService : BaseNotificationService
    {
        public DirectNotificationService(
            IConfiguration configuration,
            ILogger<DirectNotificationService> logger)
            : base(configuration, logger)
        {
        }

        public override async Task<bool> NotifyContentChangeAsync()
        {
            var notificationResult = new NotificationResult();

            try
            {
                _logger.LogInformation("Sending notification using manual-update endpoint: {Url}", _manualUpdateUrl);

                // Create a simple HttpClient without factory to avoid Plesk issues
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
    }
}