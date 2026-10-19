using System.Security.Claims;

namespace MathTestAssessment.Web.Services;

public static class AuthServiceAccessor
{
    public static bool RequiresPasswordChange(ClaimsPrincipal user) =>
        string.Equals(user.FindFirst("MustChangePassword")?.Value, "true", StringComparison.OrdinalIgnoreCase);
}
