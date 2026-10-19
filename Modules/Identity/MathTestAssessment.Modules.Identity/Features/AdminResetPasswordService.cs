using MathTestAssessment.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace MathTestAssessment.Modules.Identity.Features;

public sealed class AdminResetPasswordService(UserManager<ApplicationUser> users)
{
    public async Task<IdentityResult> ResetAsync(Guid userId, string temporaryPassword)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Description = "User account was not found." });

        var token = await users.GeneratePasswordResetTokenAsync(user);
        user.MustChangePassword = true;

        // ResetPasswordAsync updates the password hash and security stamp. The
        // changed stamp invalidates every JWT previously issued to this user.
        return await users.ResetPasswordAsync(user, token, temporaryPassword);
    }
}
