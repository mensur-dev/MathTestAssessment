using System.Text;


namespace MathTestAssessment.Modules.Examinations.Domain.Entities;

public class ExamTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ExamId { get; set; }

    public string ExternalTaskId { get; set; } = string.Empty;

    public string Expression { get; set; } = string.Empty;

    public decimal SubmittedAnswer { get; set; }

    public decimal? CorrectAnswer { get; set; }

    public bool? IsCorrect { get; set; }

    public string? EvaluationErrorCode { get; set; }
}

