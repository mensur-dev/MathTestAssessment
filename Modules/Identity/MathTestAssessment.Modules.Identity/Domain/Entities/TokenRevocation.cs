using System.Text;

namespace MathTestAssessment.Modules.Identity.Domain.Entities;
public class TokenRevocation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string JtiHash { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime RevokedAtUtc { get; set; } = DateTime.UtcNow;
}

