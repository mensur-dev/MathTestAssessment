using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MathTestAssessment.Modules.Identity.Domain.Entities;
using MathTestAssessment.Modules.Identity.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MathTestAssessment.Modules.Identity.Features;
public class LoginService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public LoginService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<LoginResultDto?> LoginAsync(LoginDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null ||
            !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return null;
        }

        return await CreateTokenAsync(user);
    }

    public async Task<LoginResultDto> CreateTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("SecurityStamp", user.SecurityStamp ?? string.Empty)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        if (user.StudentId.HasValue)
            claims.Add(new Claim("StudentId", user.StudentId.Value.ToString()));

        if (!string.IsNullOrWhiteSpace(user.ExternalTeacherId))
            claims.Add(new Claim("ExternalTeacherId", user.ExternalTeacherId));

        claims.Add(new Claim("MustChangePassword", user.MustChangePassword.ToString().ToLowerInvariant()));

        var keyValue = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(keyValue) ||
            Encoding.UTF8.GetByteCount(keyValue) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 bytes.");
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(keyValue));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddHours(2);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new LoginResultDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAt,
            MustChangePassword = user.MustChangePassword
        };
    }
}
