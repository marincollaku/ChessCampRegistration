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
        if (!TryMapRegistration(request, out var registration, out var validationError))
        {
            return BadRequest(new { message = validationError });
        }

        db.Registrations.Add(registration);
        await db.SaveChangesAsync(cancellationToken);

        QueueConfirmationEmail(registration);

        return StatusCode(StatusCodes.Status201Created, ToResponse(registration));
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
            .ToListAsync(cancellationToken);

        return Ok(registrations.Select(ToResponse));
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
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();

        return File(bytes, "text/csv; charset=utf-8", fileName);
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
        if (!TryMapRegistration(request, out var registration, out var validationError))
        {
            return BadRequest(new { message = validationError });
        }

        db.Registrations.Add(registration);
        await db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ToResponse(registration));
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

        if (!TryMapRegistration(request, out var updated, out var validationError))
        {
            return BadRequest(new { message = validationError });
        }

        registration.KidFullName = updated.KidFullName;
        registration.KidAge = updated.KidAge;
        registration.KidSchool = updated.KidSchool;
        registration.KidChessLevel = updated.KidChessLevel;
        registration.ParentName = updated.ParentName;
        registration.ParentPhone = updated.ParentPhone;
        registration.ParentEmail = updated.ParentEmail;
        registration.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(registration));
    }

    private IQueryable<Registration> ApplyFilters(string? search, string? chessLevel, int? minAge, int? maxAge)
    {
        var query = db.Registrations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(r =>
                EF.Functions.ILike(r.KidFullName, term) ||
                EF.Functions.ILike(r.KidSchool, term) ||
                EF.Functions.ILike(r.ParentName, term) ||
                EF.Functions.ILike(r.ParentEmail, term) ||
                EF.Functions.ILike(r.ParentPhone, term));
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

    private static bool TryMapRegistration(
        CreateRegistrationRequest request,
        out Registration registration,
        out string? validationError)
    {
        registration = null!;
        validationError = null;

        var kidFullName = request.KidFullName?.Trim();
        var kidSchool = request.KidSchool?.Trim();
        var kidChessLevel = request.KidChessLevel?.Trim();
        var parentName = request.ParentName?.Trim();
        var parentPhone = request.ParentPhone?.Trim();
        var parentEmail = request.ParentEmail?.Trim();

        if (string.IsNullOrWhiteSpace(kidFullName))
        {
            validationError = "Emri i fëmijës është i detyrueshëm.";
            return false;
        }

        if (request.KidAge is < 4 or > 18)
        {
            validationError = "Mosha duhet të jetë midis 4 dhe 18 vjeç.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(kidSchool))
        {
            validationError = "Shkolla është e detyrueshme.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(kidChessLevel))
        {
            validationError = "Niveli i shahut është i detyrueshëm.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(parentName))
        {
            validationError = "Emri i prindit është i detyrueshëm.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(parentPhone))
        {
            validationError = "Numri i telefonit është i detyrueshëm.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(parentEmail))
        {
            validationError = "Email-i është i detyrueshëm.";
            return false;
        }

        registration = new Registration
        {
            KidFullName = kidFullName,
            KidAge = request.KidAge,
            KidSchool = kidSchool,
            KidChessLevel = kidChessLevel,
            ParentName = parentName,
            ParentPhone = parentPhone,
            ParentEmail = parentEmail
        };

        return true;
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
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0]))
        {
            value = $"'{value}";
        }

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
