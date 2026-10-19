namespace MathTestAssessment.Modules.Examinations.Dtos;

public sealed class ExamStatisticsDto
{
    public int TotalExams { get; init; }
    public int TotalTasks { get; init; }
    public int CorrectTasks { get; init; }
    public int IncorrectTasks { get; init; }
    public int UnevaluatedTasks { get; init; }
}
