using System.Text;
using MathTestAssessment.BackgroundJobs.Domain.Enums;

namespace MathTestAssessment.BackgroundJobs.Dtos;
public class ImportJobDto
{
    public Guid Id { get; set; }
    public string? FileName { get; set; }
    public ImportJobStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}
