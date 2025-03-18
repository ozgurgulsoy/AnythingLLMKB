namespace TestKB.Models.ViewModels
{
    public class NotificationStatusViewModel
    {
        public bool IsServiceAvailable { get; set; }
        public bool LastNotificationSuccessful { get; set; }
        public string EndpointUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; }
        public int RetryCount { get; set; }
        public string LastDiagnosticResult { get; set; } = string.Empty;
        public string LastNotificationDetails { get; set; } = string.Empty;
    }
}