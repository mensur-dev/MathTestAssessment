using System.Text;
using MathTestAssessment.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace MathTestAssessment.Modules.Identity.Features;
public class AdminSeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IConfiguration _configuration;

    public AdminSeeder(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    public async Task SeedAsync()
    {
        foreach (var roleName in new[] { "Admin", "Teacher", "Student" })
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await _roleManager.CreateAsync(
                    new IdentityRole<Guid>(roleName));

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not create role '{roleName}'.");
                }
            }
        }

        var email = _configuration["SeedAdmin:Email"];
        var password = _configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "SeedAdmin email and password must be configured.");
        }

        var admin = await _userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = "System",
                LastName = "Administrator",
                MustChangePassword = false
            };

            var result = await _userManager.CreateAsync(
                admin,
                password);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ",
                        result.Errors.Select(x => x.Description)));
            }
        }

        if (!await _userManager.IsInRoleAsync(admin, "Admin"))
        {
            var result = await _userManager.AddToRoleAsync(
                admin,
                "Admin");

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Could not assign Admin role.");
            }
        }
    }
}
