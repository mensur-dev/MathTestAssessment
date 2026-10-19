using System.Security.Cryptography;
using System.Text;
using MathTestAssessment.Modules.Identity.Data;
using MathTestAssessment.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MathTestAssessment.Modules.Identity.Features;

public sealed class TokenRevocationService
{
    private readonly IdentityDbContext _dbContext;

    public TokenRevocationService(
        IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RevokeAsync(
        string jti,
        Guid userId,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            throw new ArgumentException(
                "Token identifier is required.",
                nameof(jti));
        }

        var jtiHash = ComputeHash(jti);

        var alreadyRevoked = await _dbContext.TokenRevocations
            .AnyAsync(
                x => x.JtiHash == jtiHash,
                cancellationToken);

        if (alreadyRevoked)
            return;

        var tokenRevocation = new TokenRevocation
        {
            JtiHash = jtiHash,
            UserId = userId,
            ExpiresAtUtc = expiresAtUtc,
            RevokedAtUtc = DateTime.UtcNow
        };

        _dbContext.TokenRevocations.Add(tokenRevocation);

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent logout may have inserted
            // the same token revocation.
            _dbContext.Entry(tokenRevocation).State =
                EntityState.Detached;

            var exists = await _dbContext.TokenRevocations
                .AsNoTracking()
                .AnyAsync(
                    x => x.JtiHash == jtiHash,
                    cancellationToken);

            if (!exists)
                throw;
        }
    }

    public async Task<bool> IsRevokedAsync(
        string jti,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
            return true;

        var jtiHash = ComputeHash(jti);

        return await _dbContext.TokenRevocations
            .AsNoTracking()
            .AnyAsync(
                x => x.JtiHash == jtiHash,
                cancellationToken);
    }

    private static string ComputeHash(string jti)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(jti));

        return Convert.ToHexString(hash);
    }
}

