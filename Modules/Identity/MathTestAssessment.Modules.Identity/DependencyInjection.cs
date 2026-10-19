using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MathTestAssessment.Modules.Identity.Data;
using MathTestAssessment.Modules.Identity.Domain.Entities;
using MathTestAssessment.Modules.Identity.Features;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MathTestAssessment.Modules.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // SQL Server / Identity DbContext
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable(
                    "__EFMigrationsHistory_Identity")));

        // ASP.NET Core Identity
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;

            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        // JWT signing key
        var jwtKey = configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKey) ||
            Encoding.UTF8.GetByteCount(jwtKey) < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at least 32 bytes.");
        }

        // JWT Authentication
        services.AddAuthentication(
            JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = configuration["Jwt:Issuer"],

                        ValidateAudience = true,
                        ValidAudience = configuration["Jwt:Audience"],

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtKey)),

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),

                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = ClaimTypes.Role
                    };

                // Check JWT revocation after validation
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?
                            .FindFirst(JwtRegisteredClaimNames.Jti)?
                            .Value;
                        var userId = context.Principal?
                            .FindFirst(ClaimTypes.NameIdentifier)?
                            .Value;
                        var securityStamp = context.Principal?
                            .FindFirst("SecurityStamp")?.Value;

                        if (string.IsNullOrWhiteSpace(jti) ||
                            string.IsNullOrWhiteSpace(userId) ||
                            string.IsNullOrWhiteSpace(securityStamp))
                        {
                            context.Fail(
                                "Missing required token claims.");
                            return;
                        }

                        try
                        {
                            var revocationService =
                                context.HttpContext
                                    .RequestServices
                                    .GetRequiredService<
                                        TokenRevocationService>();

                            var isRevoked = await revocationService
                                .IsRevokedAsync(
                                    jti,
                                    context.HttpContext.RequestAborted);

                            var userManager = context.HttpContext.RequestServices
                                .GetRequiredService<UserManager<ApplicationUser>>();
                            var user = await userManager.FindByIdAsync(userId);

                            if (isRevoked || user is null ||
                                !string.Equals(user.SecurityStamp, securityStamp, StringComparison.Ordinal))
                            {
                                context.Fail(
                                    "Token is no longer valid.");
                            }
                        }
                        catch (Exception ex)
                        {
                            var logger = context.HttpContext
                                .RequestServices
                                .GetRequiredService<
                                    ILogger<TokenRevocationService>>();

                            logger.LogError(
                                ex,
                                "JWT revocation validation failed.");

                            // Fail closed
                            context.Fail(
                                "Token validation failed.");
                        }
                    }
                };
            });

        // Authorization
        services.AddAuthorization();

        // Identity services
        services.AddScoped<RegisterUserService>();
        services.AddScoped<LoginService>();
        services.AddScoped<ChangePasswordService>();
        services.AddScoped<AdminResetPasswordService>();
        services.AddScoped<TokenRevocationService>();
        services.AddScoped<AdminSeeder>();

        return services;
    }
}
