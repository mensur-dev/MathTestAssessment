using System.Text;
using MathTestAssessment.BackgroundJobs.Domain.Enums;

namespace MathTestAssessment.BackgroundJobs.Domain.Entities;

public class ImportJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TeacherId { get; set; }

    public string ExternalTeacherId { get; set; } = "";

    public string FilePath { get; set; } = "";
    public string? FileName { get; set; }

    public ImportJobStatus Status { get; set; } = ImportJobStatus.Pending;

    public int AttemptCount { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime AvailableAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public Guid? LeaseToken { get; set; }

    public DateTime? LeaseExpiresAtUtc { get; set; }

    public string? ErrorMessage { get; set; }
}
