using System.Text;

namespace MathTestAssessment.Web.Models;

public class ExamResultDto
{
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }
    public int TotalTasks { get; set; }
    public int CorrectTasks { get; set; }
    public int IncorrectTasks { get; set; }
    public int UnevaluatedTasks { get; set; }
    public decimal ScorePercentage { get; set; }
}

