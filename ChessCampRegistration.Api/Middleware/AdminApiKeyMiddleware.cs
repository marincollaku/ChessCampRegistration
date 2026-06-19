namespace ChessCampRegistration.Api.Middleware;

public class AdminApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context);
            return;
        }

        if (!context.Request.Path.StartsWithSegments("/api/admin"))
        {
            await next(context);
            return;
        }

        var configuredKey = configuration["Admin:ApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { message = "Aksesi i administratorit nuk është konfiguruar." });
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Admin-Key", out var providedKey) ||
            !string.Equals(providedKey.ToString(), configuredKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Çelësi i administratorit është i pavlefshëm." });
            return;
        }

        await next(context);
    }
}
