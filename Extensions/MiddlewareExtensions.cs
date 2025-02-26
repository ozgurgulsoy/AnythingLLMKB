using TestKB.Middleware;

namespace TestKB.Extensions;

/// <summary>
/// Middleware extension metotları.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Global istisna yakalama middleware'ini ekler.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}