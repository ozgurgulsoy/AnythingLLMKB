using System;
using System.Net;
using System.Text;

namespace TestKB.Services.Notification
{
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