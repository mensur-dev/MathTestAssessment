using System.Text;

namespace MathTestAssessment.Modules.Identity.Dtos;
public class RegisterUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ExternalTeacherId { get; set; }
    public Guid? StudentId { get; set; }
}
