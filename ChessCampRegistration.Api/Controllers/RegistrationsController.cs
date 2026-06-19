using System.Text;
using ChessCampRegistration.Api.Data;
using ChessCampRegistration.Api.DTOs;
using ChessCampRegistration.Api.Models;
using ChessCampRegistration.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChessCampRegistration.Api.Controllers;

[ApiController]
[Route("api")]
public class RegistrationsController(
    AppDbContext db,
    IServiceScopeFactory scopeFactory,
    ILogger<RegistrationsController> logger) : ControllerBase
{
    [HttpPost("registrations")]
    public async Task<ActionResult<RegistrationResponse>> Create(
        [FromBody] CreateRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var registration = new Registration
        {
            KidFullName = request.KidFullName.Trim(),
            KidAge = request.KidAge,
            KidSchool = request.KidSchool.Trim(),
            KidChessLevel = request.KidChessLevel.Trim(),
            ParentName = request.ParentName.Trim(),
            ParentPhone = request.ParentPhone.Trim(),
            ParentEmail = request.ParentEmail.Trim()
        };

        db.Registrations.Add(registration);
        await db.SaveChangesAsync(cancellationToken);

        QueueConfirmationEmail(registration);

        return CreatedAtAction(nameof(GetById), new { id = registration.Id }, ToResponse(registration));
    }

    [HttpGet("admin/registrations")]
    public async Task<ActionResult<IEnumerable<RegistrationResponse>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? chessLevel,
        [FromQuery] int? minAge,
        [FromQuery] int? maxAge,
        CancellationToken cancellationToken)
    {
        var registrations = await ApplyFilters(search, chessLevel, minAge, maxAge)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToResponse(r))
            .ToListAsync(cancellationToken);

        return Ok(registrations);
    }

    [HttpGet("admin/registrations/export")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] string? search,
        [FromQuery] string? chessLevel,
        [FromQuery] int? minAge,
        [FromQuery] int? maxAge,
        CancellationToken cancellationToken)
    {
        var registrations = await ApplyFilters(search, chessLevel, minAge, maxAge)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var csv = BuildCsv(registrations);
        var fileName = $"regjistrimet-kampi-shahut-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    [HttpGet("admin/registrations/{id:int}")]
    public async Task<ActionResult<RegistrationResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var registration = await db.Registrations.FindAsync([id], cancellationToken);
        if (registration is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(registration));
    }

    [HttpPost("admin/registrations")]
    public async Task<ActionResult<RegistrationResponse>> CreateManual(
        [FromBody] CreateRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var registration = new Registration
        {
            KidFullName = request.KidFullName.Trim(),
            KidAge = request.KidAge,
            KidSchool = request.KidSchool.Trim(),
            KidChessLevel = request.KidChessLevel.Trim(),
            ParentName = request.ParentName.Trim(),
            ParentPhone = request.ParentPhone.Trim(),
            ParentEmail = request.ParentEmail.Trim()
        };

        db.Registrations.Add(registration);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = registration.Id }, ToResponse(registration));
    }

    [HttpPut("admin/registrations/{id:int}")]
    public async Task<ActionResult<RegistrationResponse>> Update(
        int id,
        [FromBody] UpdateRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var registration = await db.Registrations.FindAsync([id], cancellationToken);
        if (registration is null)
        {
            return NotFound();
        }

        registration.KidFullName = request.KidFullName.Trim();
        registration.KidAge = request.KidAge;
        registration.KidSchool = request.KidSchool.Trim();
        registration.KidChessLevel = request.KidChessLevel.Trim();
        registration.ParentName = request.ParentName.Trim();
        registration.ParentPhone = request.ParentPhone.Trim();
        registration.ParentEmail = request.ParentEmail.Trim();
        registration.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(registration));
    }

    private IQueryable<Registration> ApplyFilters(string? search, string? chessLevel, int? minAge, int? maxAge)
    {
        var query = db.Registrations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                r.KidFullName.Contains(term) ||
                r.KidSchool.Contains(term) ||
                r.ParentName.Contains(term) ||
                r.ParentEmail.Contains(term) ||
                r.ParentPhone.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(chessLevel))
        {
            query = query.Where(r => r.KidChessLevel == chessLevel);
        }

        if (minAge.HasValue)
        {
            query = query.Where(r => r.KidAge >= minAge.Value);
        }

        if (maxAge.HasValue)
        {
            query = query.Where(r => r.KidAge <= maxAge.Value);
        }

        return query;
    }

    private static string BuildCsv(IEnumerable<Registration> registrations)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Id,Emri i fëmijës,Mosha,Shkolla,Niveli i shahut,Emri i prindit,Telefoni,Email,Data e regjistrimit,Data e përditësimit");

        foreach (var registration in registrations)
        {
            builder.AppendLine(string.Join(",",
                registration.Id,
                Csv(registration.KidFullName),
                registration.KidAge,
                Csv(registration.KidSchool),
                Csv(registration.KidChessLevel),
                Csv(registration.ParentName),
                Csv(registration.ParentPhone),
                Csv(registration.ParentEmail),
                Csv(registration.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(registration.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "")));
        }

        return builder.ToString();
    }

    private static string Csv(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private void QueueConfirmationEmail(Registration registration)
    {
        var emailRegistration = new Registration
        {
            Id = registration.Id,
            KidFullName = registration.KidFullName,
            KidAge = registration.KidAge,
            KidSchool = registration.KidSchool,
            KidChessLevel = registration.KidChessLevel,
            ParentName = registration.ParentName,
            ParentPhone = registration.ParentPhone,
            ParentEmail = registration.ParentEmail,
            CreatedAt = registration.CreatedAt
        };

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                using var emailTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await emailService.SendRegistrationConfirmationAsync(emailRegistration, emailTimeout.Token);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Email send failed for registration {RegistrationId}.", registration.Id);
            }
        });
    }

    private static RegistrationResponse ToResponse(Registration registration) =>
        new(
            registration.Id,
            registration.KidFullName,
            registration.KidAge,
            registration.KidSchool,
            registration.KidChessLevel,
            registration.ParentName,
            registration.ParentPhone,
            registration.ParentEmail,
            registration.CreatedAt,
            registration.UpdatedAt);
}
