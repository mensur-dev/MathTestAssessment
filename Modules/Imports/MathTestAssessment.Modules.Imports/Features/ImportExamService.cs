
using System.Text.Json;
using MathTestAssessment.Modules.Imports.Dtos;

using MathTestAssessment.Modules.Examinations.Features;
using MathTestAssessment.Modules.Examinations.Infrastructure.Data;
using MathTestAssessment.Modules.Students.Features;
using System.Globalization;
using System.Text;
using System.Data;
using Microsoft.EntityFrameworkCore;
using MathTestAssessment.Modules.Examinations.Domain.Entities;

namespace MathTestAssessment.Modules.Imports.Features;

public class ImportExamService
{
    private const int MaxExpressionLength = 4000;
    private const decimal AnswerMagnitudeLimit = 1e20m;
    private readonly XmlExamParser _xmlParser;
    private readonly GetStudentsService _studentsService;
    private readonly EvaluateExamService _evaluationService;
    private readonly ExaminationsDbContext _examinationsDbContext;

    public ImportExamService(
        XmlExamParser xmlParser,
        GetStudentsService studentsService,
        EvaluateExamService evaluationService,
        ExaminationsDbContext examinationsDbContext)
    {
        _xmlParser = xmlParser;
        _studentsService = studentsService;
        _evaluationService = evaluationService;
        _examinationsDbContext = examinationsDbContext;
    }

    public async Task ImportAsync(
        Stream xmlStream,
        Guid teacherId,
        string externalTeacherId,
        Guid importJobId,
        CancellationToken ct = default)
    {
        if (teacherId == Guid.Empty)
        {
            throw new ArgumentException("Teacher ID is required.");
        }

        if (importJobId == Guid.Empty)
        {
            throw new ArgumentException("Import Job ID is required.");
        }

        if (string.IsNullOrWhiteSpace(externalTeacherId))
        {
            throw new ArgumentException("External Teacher ID is required.");
        }

        if (xmlStream is null || !xmlStream.CanRead)
        {
            throw new ArgumentException("Invalid XML stream.");
        }

        ct.ThrowIfCancellationRequested();

        // Kontroll i hershëm për import të përfunduar.
        var alreadyProcessed = await _examinationsDbContext
            .ProcessedImports
            .AsNoTracking()
            .AnyAsync(x => x.ImportJobId == importJobId, ct);

        if (alreadyProcessed)
        {
            return;
        }

        var importedData = _xmlParser.Parse(xmlStream);

        if (importedData is null)
        {
            throw new InvalidDataException("Invalid XML content.");
        }

        if (importedData.TeacherId != externalTeacherId)
        {
            throw new InvalidDataException(
                "XML Teacher ID does not match the authenticated teacher.");
        }

        if (importedData.Students.Count == 0 ||
            importedData.Students.Any(s => s.Exams.Count == 0))
        {
            throw new InvalidDataException(
                "The XML must contain students with at least one exam each.");
        }

        var studentLookup = await _studentsService.GetIdsByExternalCodesAsync(
            importedData.Students.Select(student => student.StudentId),
            ct);

        var exams = new List<Exam>();

        var examKeys = new HashSet<(Guid StudentId, string ExamId)>();

        var missingStudents = importedData.Students
            .Select(s => s.StudentId)
            .Where(id => string.IsNullOrWhiteSpace(id) || !studentLookup.ContainsKey(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (missingStudents.Count > 0)
        {
            var identifiers = string.Join(
                ", ",
                missingStudents.Take(10)
                    .Select(id => string.IsNullOrWhiteSpace(id)
                        ? "(empty Student ID)"
                        : id[..Math.Min(id.Length, 100)]));

            var extra = missingStudents.Count > 10
                ? $" and {missingStudents.Count - 10} more"
                : "";

            throw new InvalidDataException(
                $"Student IDs not found in the database: {identifiers}{extra}. No exams were imported. Register the students or correct their ID values in the XML, then upload again.");
        }

        var studentCodesById = studentLookup.ToDictionary(
            student => student.Value,
            student => student.Key);

        foreach (var importedStudent in importedData.Students)
        {
            ct.ThrowIfCancellationRequested();

            if (!studentLookup.TryGetValue(
                    importedStudent.StudentId,
                    out var studentId))
            {
                throw new InvalidDataException(
                    $"Student '{importedStudent.StudentId}' does not exist.");
            }

            foreach (var importedExam in importedStudent.Exams)
            {
                if (string.IsNullOrWhiteSpace(importedExam.ExamId) ||
                    importedExam.ExamId.Trim().Length > 100 ||
                    importedExam.Tasks.Count == 0)
                {
                    throw new InvalidDataException(
                        "An imported exam requires an ID and at least one task.");
                }

                if (!examKeys.Add(
                        (studentId, importedExam.ExamId.Trim())))
                {
                    throw new InvalidDataException(
                        "Duplicate exam identifier in XML.");
                }

                var exam = new Exam
                {
                    StudentId = studentId,
                    TeacherId = teacherId,
                    ExternalExamId = importedExam.ExamId.Trim(),
                    Tasks = new List<ExamTask>()
                };

                var taskIds = new HashSet<string>(StringComparer.Ordinal);

                foreach (var importedTask in importedExam.Tasks)
                {
                    if (string.IsNullOrWhiteSpace(importedTask.TaskId) ||
                        importedTask.TaskId.Trim().Length > 100 ||
                        !taskIds.Add(importedTask.TaskId.Trim()))
                    {
                        throw new InvalidDataException(
                            "Duplicate task identifier in exam.");
                    }

                    var (expression, answer) = ParseTask(importedTask.Content);

                    exam.Tasks.Add(new ExamTask
                    {
                        ExternalTaskId = importedTask.TaskId.Trim(),
                        Expression = expression,
                        SubmittedAnswer = answer
                    });
                }

                exams.Add(exam);
            }
        }

        await using var transaction =
            await _examinationsDbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                ct);

        try
        {
            // Kontrolli brenda transaksionit mbetet i nevojshëm.
            alreadyProcessed = await _examinationsDbContext
                .ProcessedImports
                .AnyAsync(x => x.ImportJobId == importJobId, ct);

            if (alreadyProcessed)
            {
                await transaction.CommitAsync(ct);
                return;
            }

            var importedStudentIds = exams
                .Select(exam => exam.StudentId)
                .Distinct()
                .ToArray();

            var importedExamIds = exams
                .Select(exam => exam.ExternalExamId)
                .Distinct()
                .ToArray();

            var existingExamKeys = (await _examinationsDbContext.Exams
                    .AsNoTracking()
                    .Where(exam => exam.TeacherId == teacherId
                        && importedStudentIds.Contains(exam.StudentId)
                        && importedExamIds.Contains(exam.ExternalExamId))
                    .Select(exam => new
                    {
                        exam.StudentId,
                        exam.ExternalExamId
                    })
                    .ToListAsync(ct))
                .Select(exam => (exam.StudentId, exam.ExternalExamId))
                .ToHashSet();

            foreach (var exam in exams)
            {
                ct.ThrowIfCancellationRequested();

                if (existingExamKeys.Contains((exam.StudentId, exam.ExternalExamId)))
                {
                    throw new InvalidDataException(
                        $"Exam '{exam.ExternalExamId}' already exists for this student and teacher. No exams from this file were imported.");
                }

                _evaluationService.Evaluate(exam);
                _examinationsDbContext.Exams.Add(exam);
            }

            _examinationsDbContext.ProcessedImports.Add(
                new ProcessedImport
                {
                    ImportJobId = importJobId,
                    CompletedAtUtc = DateTime.UtcNow,
                    SummaryJson = JsonSerializer.Serialize(new ImportSummaryDto
                    {
                        StudentCount = exams
                            .Select(x => x.StudentId)
                            .Distinct()
                            .Count(),

                        ExamCount = exams.Count,

                        TaskCount = exams.Sum(x => x.Tasks.Count),

                        Exams = exams.Select(x => new ImportedExamSummaryDto
                        {
                            ExamId = x.Id,
                            ExternalExamId = x.ExternalExamId,
                            ExternalStudentId = studentCodesById[x.StudentId],
                            TaskCount = x.Tasks.Count,
                            CorrectTasks = x.Tasks.Count(t => t.IsCorrect == true),
                            IncorrectTasks = x.Tasks.Count(t => t.IsCorrect == false),
                            UnevaluatedTasks = x.Tasks.Count(t => t.IsCorrect is null)
                        }).ToList()
                    })
                });

            await _examinationsDbContext.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is Microsoft.Data.SqlClient.SqlException sql
            && sql.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(CancellationToken.None);

            throw new InvalidDataException(
                "Duplicate exam or task ID. No exams from this file were imported. Use unique exam IDs for each student and teacher, and unique task IDs within each exam.",
                ex);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static (string Expression, decimal SubmittedAnswer) ParseTask(
        string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidDataException("Task content is empty.");
        }

        var parts = content.Split('=');

        if (parts.Length != 2)
        {
            throw new InvalidDataException(
                "Task must contain exactly one '=' character.");
        }

        var expression = parts[0].Trim();
        var answerText = parts[1].Trim();

        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new InvalidDataException("Expression is empty.");
        }

        if (!decimal.TryParse(
                answerText,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var submittedAnswer))
        {
            throw new InvalidDataException(
                "Invalid submitted answer.");
        }

        if (expression.Length > MaxExpressionLength)
        {
            throw new InvalidDataException(
                $"The task expression cannot exceed {MaxExpressionLength} characters.");
        }

        if (submittedAnswer <= -AnswerMagnitudeLimit || submittedAnswer >= AnswerMagnitudeLimit)
        {
            throw new InvalidDataException(
                "The submitted answer is outside the supported range: its magnitude must be less than 10^20.");
        }

        return (expression, submittedAnswer);
    }
}
