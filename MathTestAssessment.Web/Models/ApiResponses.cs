namespace MathTestAssessment.Web.Models;
public enum ImportJobStatus { Pending, Processing, Completed, Failed, NeedsReview }
public record CreatedResource(Guid Id);
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
public class ExamDashboardResponse
{
    public int TotalExams { get; set; }
    public int TotalTasks { get; set; }
    public int CorrectTasks { get; set; }
    public int DistinctStudents { get; set; }
    public List<ExamDto> RecentExams { get; set; } = [];
}
