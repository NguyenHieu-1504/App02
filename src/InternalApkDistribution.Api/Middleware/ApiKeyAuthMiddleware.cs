using System.Text.Json;

namespace InternalApkDistribution.Api.Middleware;

public sealed class ApiKeyAuthMiddleware
{
    private const string HeaderName = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public ApiKeyAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresApiKey(context))
        {
            await _next(context);
            return;
        }

        var expectedApiKey = _configuration["ApiSecurity:ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            await _next(context);
            return;
        }

        var providedApiKey = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
            providedApiKey = context.Request.Query["apiKey"].FirstOrDefault();

        if (!string.Equals(providedApiKey, expectedApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var body = JsonSerializer.Serialize(new { error = "Unauthorized", code = "Unauthorized" });
            await context.Response.WriteAsync(body);
            return;
        }

        await _next(context);
    }

    private static bool RequiresApiKey(HttpContext context)
    {
        var path = context.Request.Path;

        if (HttpMethods.IsPost(context.Request.Method) && path.Equals("/api/apk/upload", StringComparison.OrdinalIgnoreCase))
            return true;

        if (HttpMethods.IsDelete(context.Request.Method) && path.StartsWithSegments("/api/apk", StringComparison.OrdinalIgnoreCase))
            return true;

        if (HttpMethods.IsGet(context.Request.Method) && path.StartsWithSegments("/api/apk/download", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
