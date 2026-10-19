using System.Security.Claims;
using MathTestAssessment.BackgroundJobs.Features;
using MathTestAssessment.Modules.Identity.Domain.Entities;
using MathTestAssessment.Modules.Identity.Dtos;
using MathTestAssessment.Modules.Identity.Features;
using MathTestAssessment.Modules.Identity.Data;
using MathTestAssessment.Modules.Students.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathTestAssessment.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController(
    ImportJobService jobs,
    UserManager<ApplicationUser> users,
    IdentityDbContext identityDb,
    AdminResetPasswordService passwordReset,
    ILogger<AdminController> logger) : ControllerBase
{
    [HttpGet("import-jobs")]
    public async Task<IActionResult> ListJobs(int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        if (page < 1 || page > 100000 || pageSize < 1 || pageSize > 100) return BadRequest();
        var result = await jobs.ListForAdminAsync(page, pageSize, ct);
        return Ok(new { result.Items, result.TotalCount });
    }

    [HttpPost("import-jobs/{id:guid}/restart")]
    public async Task<IActionResult> Restart(Guid id, CancellationToken ct)
    {
        if (!await jobs.RestartFailedAsync(id, ct))
            return Conflict(new { error = "Only an existing Failed job can be restarted. Refresh its status." });
        logger.LogInformation("Admin {UserId} restarted failed import {JobId}", User.FindFirstValue(ClaimTypes.NameIdentifier), id);
        return Accepted(new { id, status = "Pending" });
    }

    // Limited directory for linking an account to an existing student.
    // This does not grant access to student management or exam data.
    [HttpGet("student-options")]
    public async Task<IActionResult> StudentOptions(
        string? search,
        CancellationToken ct)
    {
        // Cross-module read projection belongs in the API composition layer.
        // Exclude linked students in SQL before ordering and limiting the results.
        var query = identityDb.Database.SqlQuery<StudentDto>($"""
            SELECT s.[InternalId] AS [Id], s.[ID] AS [ExternalStudentId], s.[FirstName], s.[LastName]
            FROM [dbo].[Students] AS s
            WHERE NOT EXISTS (
                SELECT 1 FROM [dbo].[AspNetUsers] AS u WHERE u.[StudentId] = s.[InternalId]
            )
            """);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(student => student.ExternalStudentId.Contains(term)
                || student.FirstName.Contains(term) || student.LastName.Contains(term));
        }

        return Ok(await query.OrderBy(student => student.FirstName)
            .ThenBy(student => student.LastName).ThenBy(student => student.Id)
            .Take(100).ToListAsync(ct));
    }

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(
        int page = 1,
        int pageSize = 25,
        string? role = null,
        CancellationToken ct = default)
    {
        if (page < 1 || page > 100000 || pageSize < 1 || pageSize > 100) return BadRequest();
        if (role is not null && role is not ("Teacher" or "Student"))
            return BadRequest(new { error = "Role must be Teacher or Student." });

        var accountQuery = users.Users.AsNoTracking();
        if (role is not null)
        {
            var normalizedRole = role.ToUpperInvariant();
            accountQuery = accountQuery.Where(user => identityDb.UserRoles.Any(userRole =>
                userRole.UserId == user.Id
                && identityDb.Roles.Any(identityRole =>
                    identityRole.Id == userRole.RoleId
                    && identityRole.NormalizedName == normalizedRole)));
        }

        var totalCount = await accountQuery.CountAsync(ct);
        var accounts = await accountQuery
            .OrderBy(user => user.Email)
            .ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var accountIds = accounts.Select(account => account.Id).ToArray();
        var roleRows = await (
                from userRole in identityDb.UserRoles.AsNoTracking()
                join identityRole in identityDb.Roles.AsNoTracking()
                    on userRole.RoleId equals identityRole.Id
                where accountIds.Contains(userRole.UserId)
                select new { userRole.UserId, Role = identityRole.Name! })
            .ToListAsync(ct);
        var rolesByUser = roleRows
            .GroupBy(row => row.UserId)
            .ToDictionary(group => group.Key, group => group.Select(row => row.Role).ToArray());

        var result = accounts.Select(user => new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.ExternalTeacherId,
            user.StudentId,
            Roles = rolesByUser.GetValueOrDefault(user.Id, [])
        });
        return Ok(new { Items = result, TotalCount = totalCount });
    }

    [HttpPost("users/reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] AdminResetPasswordDto request)
    {
        if (request.UserId == Guid.Empty)
            return BadRequest(new { error = "Select a user account." });

        var result = await passwordReset.ResetAsync(request.UserId, request.TemporaryPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(x => x.Description) });

        logger.LogInformation(
            "Admin {AdminId} reset the password for user {UserId}",
            User.FindFirstValue(ClaimTypes.NameIdentifier), request.UserId);

        return NoContent();
    }

}

