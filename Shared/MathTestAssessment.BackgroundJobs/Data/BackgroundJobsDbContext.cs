using MathTestAssessment.BackgroundJobs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using System.Text;

namespace MathTestAssessment.BackgroundJobs.Data;
public class BackgroundJobsDbContext : DbContext
{
    public BackgroundJobsDbContext(
        DbContextOptions<BackgroundJobsDbContext> options)
        : base(options)
    {
    }
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ImportJob>();

        entity.ToTable("ImportJobs", "dbo");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.ExternalTeacherId)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(x => x.FilePath)
            .HasMaxLength(1000)
            .IsRequired();

        entity.Property(x => x.FileName).HasMaxLength(255);

        entity.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        entity.HasIndex(x => new
        {
            x.Status,
            x.AvailableAtUtc
        });

        entity.HasIndex(x => new
        {
            x.Status,
            x.LeaseExpiresAtUtc
        });

        entity.HasIndex(x => x.TeacherId);
    }
}

