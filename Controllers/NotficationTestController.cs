// Add this to a new file: Controllers/NotificationTestController.cs
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TestKB.Services.Interfaces;

namespace TestKB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationTestController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationTestController> _logger;

        public NotificationTestController(
            INotificationService notificationService,
            ILogger<NotificationTestController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        // GET: api/NotificationTest/Send
        [HttpGet("Send")]
        public async Task<IActionResult> TestNotification()
        {
            try
            {
                _logger.LogInformation("Manual notification requested");
                bool result = await _notificationService.NotifyContentChangeAsync();

                if (result)
                {
                    return Ok(new { status = "success", message = "Notification sent successfully" });
                }
                else
                {
                    return BadRequest(new { status = "error", message = "Failed to send notification" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test notification");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }

        // GET: api/NotificationTest/Raw
        [HttpGet("Raw")]
        public ContentResult GetRawContent()
        {
            // This endpoint returns the converted content without security checks
            // It uses the same logic as ConvertedContent but with CORS allowed
            var jsonFilePath = Path.Combine(
                HttpContext.RequestServices.GetService<IWebHostEnvironment>().WebRootPath,
                "data.json");

            string content = "No content available";

            if (System.IO.File.Exists(jsonFilePath))
            {
                content = System.IO.File.ReadAllText(jsonFilePath, System.Text.Encoding.UTF8);
                content = Services.JsonToTextConverter.ConvertJsonToText(content);
            }

            // Set headers to allow access from any origin
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Headers.Add("Access-Control-Allow-Methods", "GET");

            return Content(content, "text/plain", System.Text.Encoding.UTF8);
        }
    }
}