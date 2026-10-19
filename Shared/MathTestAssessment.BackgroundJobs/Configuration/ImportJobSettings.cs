using System.Text;
namespace MathTestAssessment.BackgroundJobs.Configuration;
public class ImportJobSettings
{
    public int MaxAttempts { get; set; } = 5;
    public int MaxParallelJobs { get; set; } = 4;
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan PollInterval { get; set; } =TimeSpan.FromSeconds(2);
}