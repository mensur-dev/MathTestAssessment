using System.Text;
using MathTestAssessment.Modules.Identity.Domain.Entities;
using MathTestAssessment.Modules.Identity.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace MathTestAssessment.Modules.Identity.Features;

public class RegisterUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public RegisterUserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IdentityResult> RegisterAsync(RegisterUserDto request)
    {
        if (request.Role != "Teacher" && request.Role != "Student")
            throw new ArgumentException("Invalid role.");

        if (request.Role == "Teacher" &&
            string.IsNullOrWhiteSpace(request.ExternalTeacherId))
            throw new ArgumentException("ID is required.");

        var teacherCode = request.Role == "Teacher"
            ? request.ExternalTeacherId!.Trim()
            : null;

        if (teacherCode?.Length > 100)
            throw new ArgumentException("ID cannot exceed 100 characters.");

        if (teacherCode is not null &&
            await _userManager.Users.AnyAsync(x => x.ExternalTeacherId == teacherCode))
            throw new ArgumentException("This ID is already assigned to another teacher.");

        if (request.Role == "Student" &&
            (!request.StudentId.HasValue || request.StudentId == Guid.Empty))
            throw new ArgumentException("Student ID is required.");

        if (request.Role == "Student" &&
            await _userManager.Users.AnyAsync(x => x.StudentId == request.StudentId))
            throw new ArgumentException("The selected student already has a user account.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            ExternalTeacherId = request.Role == "Teacher"
                ? teacherCode
                : null,
            StudentId = request.Role == "Student"
                ? request.StudentId
                : null,
            MustChangePassword = true
        };

        IdentityResult result;
        try
        {
            result = await _userManager.CreateAsync(user, request.Password);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ArgumentException(
                request.Role == "Teacher"
                    ? "This ID is already assigned to another teacher."
                    : "The selected student already has a user account.");
        }

        if (!result.Succeeded)
            return result;

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            var roleResult = await _roleManager.CreateAsync(
                new IdentityRole<Guid>(request.Role));

            if (!roleResult.Succeeded)
                return roleResult;
        }

        return await _userManager.AddToRoleAsync(user, request.Role);
    }
}

