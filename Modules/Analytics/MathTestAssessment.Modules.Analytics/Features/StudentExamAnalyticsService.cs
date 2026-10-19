using System.Text;
using MathTestAssessment.Modules.Examinations.Dtos;
using MathTestAssessment.Modules.Examinations.Features;

namespace MathTestAssessment.Modules.Analytics.Features;
public class StudentExamAnalyticsService
{
    private readonly GetExamsService _getExamsService;

    public StudentExamAnalyticsService(GetExamsService getExamsService)
    {
        _getExamsService = getExamsService;
    }

    public async Task<ExamDto?> GetExamAsync(Guid studentId, Guid examId)
    {
        if (studentId == Guid.Empty || examId == Guid.Empty)
            throw new ArgumentException("Student ID and Exam ID are required.");

        var exam = await _getExamsService.GetByIdAsync(examId);

        if (exam == null || exam.StudentId != studentId)
            return null;

        return exam;
    }
}

