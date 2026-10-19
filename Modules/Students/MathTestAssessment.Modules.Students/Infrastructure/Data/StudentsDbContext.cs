using MathTestAssessment.Modules.Students.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using System.Text;

namespace MathTestAssessment.Modules.Students.Infrastructure.Data;

public class StudentsDbContext : DbContext
{
    public StudentsDbContext(
        DbContextOptions<StudentsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");

            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("InternalId");

            entity.Property(x => x.ExternalStudentId)
                .HasColumnName("ID")
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.ExternalStudentId)
                .IsUnique();

            entity.Property(x => x.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.LastName)
                .HasMaxLength(100)
                .IsRequired();
        });
    }
}

