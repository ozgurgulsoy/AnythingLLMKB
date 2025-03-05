using System.Text.Json;
using TestKB.Services;
using TestKB.Services.Interfaces;

namespace TestKB.Middleware
{

    public class GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IErrorHandlingService errorHandlingService)
    {
        private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IErrorHandlingService _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İstek işlenirken beklenmeyen bir hata oluştu: {Path}", context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var errorResponse = _errorHandlingService.HandleException(ex, context.Request.Path);

            // Hata kodlarına göre HTTP durum kodunu ayarla
            context.Response.StatusCode = errorResponse.ErrorCode switch
            {
                ErrorCode.ValidationError => StatusCodes.Status400BadRequest,
                ErrorCode.InvalidArgument => StatusCodes.Status400BadRequest,
                ErrorCode.NotFound => StatusCodes.Status404NotFound,
                ErrorCode.Unauthorized => StatusCodes.Status401Unauthorized,
                ErrorCode.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };

            var result = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(result);
        }
    }
}