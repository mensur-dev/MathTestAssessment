using System.Text;

namespace MathTestAssessment.Modules.Examinations.Domain.Entities;

public class ProcessedImport
{
    public Guid ImportJobId { get; set; }

    public DateTime CompletedAtUtc { get; set; }
    public string? SummaryJson { get; set; }
}
