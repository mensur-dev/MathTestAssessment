using System.Text;

namespace MathTestAssessment.Web.Models;
public class StudentAnalyticsDto
{
    public Guid StudentId { get; set; }
    public int TotalExams { get; set; }
    public int TotalTasks { get; set; }
    public int CorrectTasks { get; set; }
    public int IncorrectTasks { get; set; }
    public int UnevaluatedTasks { get; set; }
    public decimal AverageScorePercentage { get; set; }
}


