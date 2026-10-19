using System.Text;
using MathTestAssessment.Modules.Analytics.Dtos;
using MathTestAssessment.Modules.Examinations.Dtos;
using MathTestAssessment.Modules.Examinations.Features;

namespace MathTestAssessment.Modules.Analytics.Features;

public class StudentAnalyticsService
{
    private readonly GetExamsService _getExamsService;

    public StudentAnalyticsService(GetExamsService getExamsService)
    {
        _getExamsService = getExamsService;
    }

    public async Task<StudentAnalyticsDto> GetAsync(Guid studentId)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("Student ID is required.");

        var statistics = await _getExamsService.GetStatisticsByStudentAsync(studentId);

        return Calculate(studentId, statistics);
    }

    public async Task<StudentAnalyticsDto> GetByTeacherAsync(
        Guid studentId,
        Guid teacherId)
    {
        if (studentId == Guid.Empty)
            throw new ArgumentException("Student ID is required.");

        if (teacherId == Guid.Empty)
            throw new ArgumentException("Teacher ID is required.");

        var statistics = await _getExamsService.GetStatisticsByTeacherAndStudentAsync(
            teacherId,
            studentId);

        return Calculate(studentId, statistics);
    }

    private static StudentAnalyticsDto Calculate(
        Guid studentId,
        ExamStatisticsDto statistics)
    {
        var result = new StudentAnalyticsDto
        {
            StudentId = studentId,
            TotalExams = statistics.TotalExams,
            TotalTasks = statistics.TotalTasks,
            CorrectTasks = statistics.CorrectTasks,
            IncorrectTasks = statistics.IncorrectTasks,
            UnevaluatedTasks = statistics.UnevaluatedTasks
        };

        var evaluatedTasks =
            result.CorrectTasks + result.IncorrectTasks;

        if (evaluatedTasks > 0)
        {
            result.AverageScorePercentage = Math.Round(
                (decimal)result.CorrectTasks / evaluatedTasks * 100,
                2);
        }

        return result;
    }
}

