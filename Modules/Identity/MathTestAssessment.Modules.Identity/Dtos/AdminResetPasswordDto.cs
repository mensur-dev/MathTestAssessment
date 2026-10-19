namespace MathTestAssessment.Modules.Identity.Dtos;

public sealed class AdminResetPasswordDto
{
    public Guid UserId { get; set; }
    public string TemporaryPassword { get; set; } = string.Empty;
}
