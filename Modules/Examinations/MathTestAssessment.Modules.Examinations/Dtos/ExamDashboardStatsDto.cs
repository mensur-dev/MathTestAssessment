namespace MathTestAssessment.Modules.Examinations.Dtos;

public sealed class ExamDashboardStatsDto
{
    public int TotalExams { get; init; }
    public int TotalTasks { get; init; }
    public int CorrectTasks { get; init; }
    public int DistinctStudents { get; init; }
}
