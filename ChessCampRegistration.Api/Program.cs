using ChessCampRegistration.Api.Data;
using ChessCampRegistration.Api.Middleware;
using ChessCampRegistration.Api.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://+:{port}");

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = ResolveConnectionString(builder.Configuration);
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IEmailService, EmailService>();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins is null || corsOrigins.Length == 0)
{
    var corsOrigin = builder.Configuration["CORS_ORIGIN"];
    corsOrigins = string.IsNullOrWhiteSpace(corsOrigin)
        ? ["http://localhost:5173"]
        : [corsOrigin];
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();
app.UseMiddleware<AdminApiKeyMiddleware>();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed.");
        }
    });
});

app.Run();

static string ResolveConnectionString(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? configuration["DATABASE_URL"]
        ?? Environment.GetEnvironmentVariable("DATABASE_URL");

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
