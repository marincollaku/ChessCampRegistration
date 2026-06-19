namespace ChessCampRegistration.Api.Models;

public class Registration
{
    public int Id { get; set; }
    public required string KidFullName { get; set; }
    public int KidAge { get; set; }
    public required string KidSchool { get; set; }
    public required string KidChessLevel { get; set; }
    public required string ParentName { get; set; }
    public required string ParentPhone { get; set; }
    public required string ParentEmail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
