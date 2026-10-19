using MathTestAssessment.Modules.Identity.Domain.Entities;
using MathTestAssessment.Modules.Identity.Dtos;
using Microsoft.AspNetCore.Identity;

namespace MathTestAssessment.Modules.Identity.Features;

public sealed class ChangePasswordService(
    UserManager<ApplicationUser> users,
    LoginService login)
{
    public async Task<(IdentityResult Result, LoginResultDto? Session)> ChangeAsync(
        Guid userId,
        string currentPassword,
        string newPassword)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
            return (IdentityResult.Failed(new IdentityError { Description = "User account was not found." }), null);

        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
            return (IdentityResult.Failed(new IdentityError { Description = "The new password must be different from the current password." }), null);

        // ChangePasswordAsync persists the tracked user, so the password hash and
        // first-login flag are committed together in one Identity update.
        user.MustChangePassword = false;
        var result = await users.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            return (result, null);

        return (IdentityResult.Success, await login.CreateTokenAsync(user));
    }
}
