using System.ComponentModel.DataAnnotations;

namespace ChessCampRegistration.Api.DTOs;

public record RegistrationResponse(
    int Id,
    string KidFullName,
    int KidAge,
    string KidSchool,
    string KidChessLevel,
    string ParentName,
    string ParentPhone,
    string ParentEmail,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public class CreateRegistrationRequest
{
    [Required, MaxLength(200)]
    public string KidFullName { get; set; } = string.Empty;

    [Range(4, 18)]
    public int KidAge { get; set; }

    [Required, MaxLength(200)]
    public string KidSchool { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string KidChessLevel { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string ParentName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string ParentPhone { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string ParentEmail { get; set; } = string.Empty;
}

public class UpdateRegistrationRequest : CreateRegistrationRequest;
