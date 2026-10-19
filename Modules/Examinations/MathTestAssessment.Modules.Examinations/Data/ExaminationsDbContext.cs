using System.Text;
using MathTestAssessment.Modules.Examinations.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MathTestAssessment.Modules.Examinations.Infrastructure.Data;

public class ExaminationsDbContext : DbContext
{
    public ExaminationsDbContext(
        DbContextOptions<ExaminationsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamTask> ExamTasks => Set<ExamTask>();
    public DbSet<ProcessedImport> ProcessedImports => Set<ProcessedImport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Exam>(entity =>
        {
            entity.ToTable("Exams");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.ExternalExamId)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.StudentId);
            entity.HasIndex(x => x.TeacherId);
            entity.HasIndex(x => new { x.TeacherId, x.StudentId, x.ExternalExamId }).IsUnique();

            entity.HasMany(x => x.Tasks)
                .WithOne()
                .HasForeignKey(x => x.ExamId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamTask>(entity =>
        {
            entity.ToTable("ExamTasks");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.ExternalTaskId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Expression)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(x => x.SubmittedAnswer)
                .HasColumnType("decimal(38,18)");

            entity.Property(x => x.CorrectAnswer)
                .HasColumnType("decimal(38,18)");

            entity.Property(x => x.EvaluationErrorCode)
                .HasMaxLength(100);

            entity.HasIndex(x => new
            {
                x.ExamId,
                x.ExternalTaskId
            }).IsUnique();
        });
        modelBuilder.Entity<ProcessedImport>(entity =>
        {
            entity.ToTable("ProcessedImports", "dbo");

            entity.HasKey(x => x.ImportJobId);

            entity.Property(x => x.CompletedAtUtc)
                .IsRequired();
        });
    }
}



