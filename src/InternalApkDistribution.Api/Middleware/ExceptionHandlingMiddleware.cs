using System.Net;
using System.Text.Json;

namespace InternalApkDistribution.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteErrorResponseAsync(context, ex);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = MapException(ex);
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        var body = new { error = message, code = statusCode.ToString() };
        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }

    private static (HttpStatusCode statusCode, string message) MapException(Exception ex)
    {
        if (ex is InvalidOperationException iop)
        {
            if (iop.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                iop.Message.Contains("Version already exists", StringComparison.OrdinalIgnoreCase))
                return (HttpStatusCode.Conflict, "Phiên bản này đã tồn tại (trùng packageName + versionCode).");
            if (iop.Message.Contains("Invalid APK", StringComparison.OrdinalIgnoreCase))
                return (HttpStatusCode.BadRequest, iop.Message);
        }
        if (ex is ArgumentException)
            return (HttpStatusCode.BadRequest, ex.Message);
        return (HttpStatusCode.InternalServerError, "Đã xảy ra lỗi. Vui lòng thử lại sau.");
    }
}
