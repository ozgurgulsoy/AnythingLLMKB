// Create a new file: Controllers/ContentApiController.cs
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Text;

namespace TestKB.Controllers
{
    [Route("api/content")]
    [ApiController]
    public class ContentApiController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ContentApiController> _logger;

        public ContentApiController(
            IWebHostEnvironment env,
            ILogger<ContentApiController> logger)
        {
            _env = env;
            _logger = logger;
        }

        // GET: api/content/data
        [HttpGet("data")]
        public IActionResult GetData()
        {
            try
            {
                _logger.LogInformation("Content data API request received");

                // Try both possible locations for the data file
                var jsonFilePath = Path.Combine(_env.WebRootPath, "data.json");
                if (!System.IO.File.Exists(jsonFilePath))
                {
                    jsonFilePath = Path.Combine(_env.ContentRootPath, "App_Data", "data.json");
                    if (!System.IO.File.Exists(jsonFilePath))
                    {
                        return NotFound("Data file not found");
                    }
                }

                // Read content and convert
                string jsonContent = System.IO.File.ReadAllText(jsonFilePath, Encoding.UTF8);
                string plainText = Services.JsonToTextConverter.ConvertJsonToText(jsonContent);

                // Set CORS headers
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");

                return Content(plainText, "text/plain", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving content data");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // OPTIONS: api/content/data - Support for CORS preflight
        [HttpOptions("data")]
        public IActionResult OptionsForData()
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
            Response.Headers.Add("Access-Control-Allow-Headers", "*");
            return Ok();
        }
    }
}