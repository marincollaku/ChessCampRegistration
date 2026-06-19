using ChessCampRegistration.Api.Data;
using ChessCampRegistration.Api.Middleware;
using ChessCampRegistration.Api.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = ResolveConnectionString(builder.Configuration);
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddHttpClient("SendGrid", client => client.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddScoped<IEmailService, EmailService>();

var allowedOrigins = ResolveAllowedOrigins(builder.Configuration, builder.Environment.IsProduction());

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(origin => IsOriginAllowed(origin, allowedOrigins, builder.Environment.IsProduction()))
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("CORS allowed origins: {Origins}", string.Join(", ", allowedOrigins));

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    logger.LogInformation("Database migrations applied.");
}

app.UseRouting();
app.UseCors();
app.UseMiddleware<AdminApiKeyMiddleware>();
app.UseAuthorization();
app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    try
    {
        if (!await db.Database.CanConnectAsync(cancellationToken))
        {
            return Results.Json(new { status = "degraded", database = "unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Ok(new { status = "ok" });
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Health check failed.");
        return Results.Json(new { status = "degraded", database = "error" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});
app.MapControllers();

app.Run();

static string[] ResolveAllowedOrigins(IConfiguration configuration, bool isProduction)
{
    var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    var fromConfig = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (fromConfig is not null)
    {
        foreach (var origin in fromConfig)
        {
            origins.Add(NormalizeOrigin(origin));
        }
    }

    var corsOrigin = configuration["CORS_ORIGIN"]
        ?? Environment.GetEnvironmentVariable("CORS_ORIGIN");

    if (!string.IsNullOrWhiteSpace(corsOrigin))
    {
        foreach (var origin in corsOrigin.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            origins.Add(NormalizeOrigin(origin));
        }
    }

    origins.Add(NormalizeOrigin("http://localhost:5173"));

    if (isProduction)
    {
        origins.Add(NormalizeOrigin("https://chess-camp-web.onrender.com"));
    }

    return origins.ToArray();
}

static bool IsOriginAllowed(string origin, IEnumerable<string> allowedOrigins, bool isProduction)
{
    if (string.IsNullOrWhiteSpace(origin))
    {
        return false;
    }

    var normalized = NormalizeOrigin(origin);
    if (allowedOrigins.Contains(normalized, StringComparer.OrdinalIgnoreCase))
    {
        return true;
    }

    if (!isProduction &&
        Uri.TryCreate(normalized, UriKind.Absolute, out var localUri) &&
        (localUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
         localUri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)))
    {
        return true;
    }

    if (isProduction &&
        Uri.TryCreate(normalized, UriKind.Absolute, out var uri) &&
        uri.Host.EndsWith(".onrender.com", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    return false;
}

static string NormalizeOrigin(string origin) => origin.Trim().TrimEnd('/');

static string ResolveConnectionString(IConfiguration configuration)
{
    var connectionString = configuration["DATABASE_URL"]
        ?? Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Database connection is not configured. Set DATABASE_URL or ConnectionStrings:DefaultConnection.");
    }

    connectionString = connectionString.Trim();

    if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        connectionString = FixTruncatedSslMode(connectionString);
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }

    return connectionString;
}

static string FixTruncatedSslMode(string connectionString)
{
    if (connectionString.EndsWith("?sslmode", StringComparison.OrdinalIgnoreCase))
    {
        return connectionString + "=require";
    }

    if (!connectionString.Contains("sslmode=", StringComparison.OrdinalIgnoreCase))
    {
        return connectionString.Contains('?')
            ? connectionString + "&sslmode=require"
            : connectionString + "?sslmode=require";
    }

    return connectionString;
}
