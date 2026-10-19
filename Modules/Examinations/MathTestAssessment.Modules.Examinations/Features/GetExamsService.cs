using System.Text;
using MathTestAssessment.Modules.Examinations.Dtos;
using MathTestAssessment.Modules.Examinations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MathTestAssessment.Modules.Examinations.Features;
public class GetExamsService
{
    private readonly ExaminationsDbContext _dbContext;

    public GetExamsService(ExaminationsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExamDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Exams
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ExamDto
            {
                Id = x.Id,
                StudentId = x.StudentId,
                TeacherId = x.TeacherId,
                ExternalExamId = x.ExternalExamId,
                CreatedAtUtc = x.CreatedAtUtc,
                Tasks = x.Tasks.Select(t => new ExamTaskDto
                {
                    Id = t.Id,
                    ExternalTaskId = t.ExternalTaskId,
                    Expression = t.Expression,
                    SubmittedAnswer = t.SubmittedAnswer,
                    CorrectAnswer = t.CorrectAnswer,
                    IsCorrect = t.IsCorrect,
                    EvaluationErrorCode = t.EvaluationErrorCode
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<(List<ExamDto> Items, int TotalCount)> GetPageByTeacherAsync(
        Guid teacherId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        GetPageAsync(
            _dbContext.Exams.Where(exam => exam.TeacherId == teacherId),
            page,
            pageSize,
            cancellationToken);

    public Task<(List<ExamDto> Items, int TotalCount)> GetPageByStudentAsync(
        Guid studentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        GetPageAsync(
            _dbContext.Exams.Where(exam => exam.StudentId == studentId),
            page,
            pageSize,
            cancellationToken);

    public Task<(List<ExamDto> Items, int TotalCount)> GetPageByTeacherAndStudentAsync(
        Guid teacherId,
        Guid studentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        GetPageAsync(
            _dbContext.Exams.Where(exam =>
                exam.TeacherId == teacherId && exam.StudentId == studentId),
            page,
            pageSize,
            cancellationToken);

    private static async Task<(List<ExamDto> Items, int TotalCount)> GetPageAsync(
        IQueryable<Domain.Entities.Exam> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .AsNoTracking()
            .OrderByDescending(exam => exam.CreatedAtUtc)
            .ThenBy(exam => exam.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(exam => new ExamDto
            {
                Id = exam.Id,
                StudentId = exam.StudentId,
                TeacherId = exam.TeacherId,
                ExternalExamId = exam.ExternalExamId,
                CreatedAtUtc = exam.CreatedAtUtc,
                Tasks = exam.Tasks.Select(task => new ExamTaskDto
                {
                    Id = task.Id,
                    ExternalTaskId = task.ExternalTaskId,
                    Expression = task.Expression,
                    SubmittedAnswer = task.SubmittedAnswer,
                    CorrectAnswer = task.CorrectAnswer,
                    IsCorrect = task.IsCorrect,
                    EvaluationErrorCode = task.EvaluationErrorCode
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<ExamStatisticsDto> GetStatisticsByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        GetStatisticsAsync(
            _dbContext.Exams.Where(exam => exam.StudentId == studentId),
            cancellationToken);

    public Task<ExamStatisticsDto> GetStatisticsByTeacherAndStudentAsync(
        Guid teacherId,
        Guid studentId,
        CancellationToken cancellationToken = default) =>
        GetStatisticsAsync(
            _dbContext.Exams.Where(exam =>
                exam.TeacherId == teacherId && exam.StudentId == studentId),
            cancellationToken);

    public async Task<ExamDashboardStatsDto> GetDashboardStatsByTeacherAsync(
        Guid teacherId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Exams
            .AsNoTracking()
            .Where(exam => exam.TeacherId == teacherId)
            .GroupBy(_ => 1)
            .Select(exams => new ExamDashboardStatsDto
            {
                TotalExams = exams.Count(),
                TotalTasks = exams.Sum(exam => exam.Tasks.Count),
                CorrectTasks = exams.Sum(exam => exam.Tasks.Count(task => task.IsCorrect == true)),
                DistinctStudents = exams.Select(exam => exam.StudentId).Distinct().Count()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? new ExamDashboardStatsDto();
    }

    public async Task<ExamDashboardStatsDto> GetDashboardStatsByStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Exams
            .AsNoTracking()
            .Where(exam => exam.StudentId == studentId)
            .GroupBy(_ => 1)
            .Select(exams => new ExamDashboardStatsDto
            {
                TotalExams = exams.Count(),
                TotalTasks = exams.Sum(exam => exam.Tasks.Count),
                CorrectTasks = exams.Sum(exam => exam.Tasks.Count(task => task.IsCorrect == true)),
                DistinctStudents = 1
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? new ExamDashboardStatsDto();
    }

    private static async Task<ExamStatisticsDto> GetStatisticsAsync(
        IQueryable<Domain.Entities.Exam> query,
        CancellationToken cancellationToken)
    {
        return await query
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(exams => new ExamStatisticsDto
            {
                TotalExams = exams.Count(),
                TotalTasks = exams.Sum(exam => exam.Tasks.Count),
                CorrectTasks = exams.Sum(exam => exam.Tasks.Count(task => task.IsCorrect == true)),
                IncorrectTasks = exams.Sum(exam => exam.Tasks.Count(task => task.IsCorrect == false)),
                UnevaluatedTasks = exams.Sum(exam => exam.Tasks.Count(task => task.IsCorrect == null))
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? new ExamStatisticsDto();
    }

}


