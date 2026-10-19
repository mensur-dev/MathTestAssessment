using MathTestAssessment.Api.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MathTestAssessment.Modules.Identity.Dtos;
using MathTestAssessment.Modules.Identity.Features;
using MathTestAssessment.Modules.Students.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathTestAssessment.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterUserService _registerService;
    private readonly LoginService _loginService;
    private readonly TokenRevocationService _tokenRevocationService;
    private readonly ChangePasswordService _changePasswordService;
    private readonly GetStudentsService _students;

    public AuthController(
        RegisterUserService registerService,
        LoginService loginService,
        TokenRevocationService tokenRevocationService,
        ChangePasswordService changePasswordService,
        GetStudentsService students)
    {
        _registerService = registerService;
        _loginService = loginService;
        _tokenRevocationService = tokenRevocationService;
        _changePasswordService = changePasswordService;
        _students = students;
    }

    [HttpPost("register")]
    [SerializeExternalIdWrites]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserDto request)
    {
        try
        {
            if (request.Role == "Teacher")
            {
                var teacherCode = request.ExternalTeacherId?.Trim();
                if (!string.IsNullOrWhiteSpace(teacherCode) &&
                    await _students.ExternalCodeExistsAsync(teacherCode))
                {
                    return Conflict(new { Error = "This ID is already assigned to a student." });
                }
            }

            if (request.Role == "Student")
            {
                if (!request.StudentId.HasValue || request.StudentId == Guid.Empty)
                    return BadRequest(new { Error = "Select an existing student." });

                var student = await _students.GetByIdAsync(request.StudentId.Value);
                if (student is null)
                    return BadRequest(new { Error = "The selected student does not exist." });

                request.FirstName = student.FirstName;
                request.LastName = student.LastName;
            }

            var result = await _registerService.RegisterAsync(request);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    Errors = result.Errors
                        .Select(x => x.Description)
                });
            }

            return Ok(new
            {
                Message = "User registered successfully."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Error = ex.Message
            });
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();

        var (result, session) = await _changePasswordService.ChangeAsync(
            userId, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
            return BadRequest(new { Errors = result.Errors.Select(x => x.Description) });

        return Ok(session);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto request)
    {
        var result = await _loginService.LoginAsync(request);

        if (result is null)
        {
            return Unauthorized(new
            {
                Message = "Invalid email or password."
            });
        }

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var jti = User.FindFirstValue(
            JwtRegisteredClaimNames.Jti);

        var expirationValue = User.FindFirstValue(
            JwtRegisteredClaimNames.Exp);

        if (!Guid.TryParse(userIdValue, out var userId) ||
            string.IsNullOrWhiteSpace(jti) ||
            !long.TryParse(expirationValue, out var expiration))
        {
            return Unauthorized();
        }

        DateTime expiresAtUtc;

        try
        {
            expiresAtUtc = DateTimeOffset
                .FromUnixTimeSeconds(expiration)
                .UtcDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            return Unauthorized();
        }

        await _tokenRevocationService.RevokeAsync(
            jti,
            userId,
            expiresAtUtc,
            cancellationToken);

        return NoContent();
    }
}
