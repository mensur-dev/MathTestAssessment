using MathTestAssessment.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using System.Text;

namespace MathTestAssessment.Modules.Identity.Data;

public class IdentityDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public IdentityDbContext(
        DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }
    public DbSet<TokenRevocation> TokenRevocations => Set<TokenRevocation>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.Id).HasColumnName("InternalId");
            entity.Property(x => x.FirstName)
                .HasMaxLength(100);

            entity.Property(x => x.LastName)
                .HasMaxLength(100);

            entity.Property(x => x.ExternalTeacherId)
                .HasColumnName("ID")
                .HasMaxLength(100);

            entity.HasIndex(x => x.ExternalTeacherId)
                .IsUnique()
                .HasFilter("[ID] IS NOT NULL");

            entity.HasIndex(x => x.StudentId)
                .IsUnique()
                .HasFilter("[StudentId] IS NOT NULL");
        });

        builder.Entity<TokenRevocation>(entity =>
        {
            entity.ToTable("TokenRevocations");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.JtiHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.HasIndex(x => x.JtiHash)
                .IsUnique();

            entity.HasIndex(x => x.ExpiresAtUtc);

            entity.Property(x => x.UserId)
                .IsRequired();

            entity.Property(x => x.ExpiresAtUtc)
                .IsRequired();

            entity.Property(x => x.RevokedAtUtc)
                .IsRequired();
        });
    }
}
