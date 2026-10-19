using System.Text;
namespace MathTestAssessment.Modules.Identity.Dtos;
public class LoginResultDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool MustChangePassword { get; set; }
}
