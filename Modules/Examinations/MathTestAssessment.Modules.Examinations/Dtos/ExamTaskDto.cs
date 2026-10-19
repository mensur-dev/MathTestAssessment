using System.Text;
namespace MathTestAssessment.Modules.Examinations.Dtos;
public class ExamTaskDto
{
    public Guid Id { get; set; }
    public string ExternalTaskId { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public decimal SubmittedAnswer { get; set; }
    public decimal? CorrectAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public string? EvaluationErrorCode { get; set; }
}