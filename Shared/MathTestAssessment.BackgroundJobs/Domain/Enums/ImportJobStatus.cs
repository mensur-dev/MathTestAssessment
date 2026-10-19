using System.Text;

namespace MathTestAssessment.BackgroundJobs.Domain.Enums
{
    public enum ImportJobStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        NeedsReview
    }
}
