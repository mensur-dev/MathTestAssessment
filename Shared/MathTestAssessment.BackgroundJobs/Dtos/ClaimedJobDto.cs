using System.Text;

namespace MathTestAssessment.BackgroundJobs.Dtos;
public class ClaimedJobDto
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string ExternalTeacherId { get; set; } = "";
    public string FilePath { get; set; } = "";
    public Guid LeaseToken { get; set; }
    public int AttemptCount { get; set; }
}
