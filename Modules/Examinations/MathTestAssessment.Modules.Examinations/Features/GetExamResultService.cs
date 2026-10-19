using MathTestAssessment.Modules.Examinations.Dtos;
using MathTestAssessment.Modules.Examinations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace MathTestAssessment.Modules.Examinations.Features;

public class GetExamResultService
{
    private readonly ExaminationsDbContext _dbContext;

    public GetExamResultService(ExaminationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExamResultDto?> GetAsync(
        Guid examId,
        CancellationToken cancellationToken = default)
    {
        var exam = await _dbContext.Exams
            .AsNoTracking()
            .Include(x => x.Tasks)
            .FirstOrDefaultAsync(
                x => x.Id == examId,
                cancellationToken);

        if (exam == null)
            return null;

        var totalTasks = exam.Tasks.Count;
        var correctTasks = exam.Tasks.Count(x => x.IsCorrect == true);
        var incorrectTasks = exam.Tasks.Count(x => x.IsCorrect == false);
        var unevaluatedTasks = exam.Tasks.Count(x => x.IsCorrect == null);

        return new ExamResultDto
        {
            ExamId = exam.Id,
            StudentId = exam.StudentId,
            TotalTasks = totalTasks,
            CorrectTasks = correctTasks,
            IncorrectTasks = incorrectTasks,
            UnevaluatedTasks = unevaluatedTasks,
            ScorePercentage = correctTasks + incorrectTasks == 0 ? 0 : Math.Round((decimal)correctTasks / (correctTasks + incorrectTasks) * 100, 2)
        };
    }
}


