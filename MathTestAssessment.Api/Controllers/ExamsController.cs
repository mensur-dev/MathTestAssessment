using System.Security.Claims;
using MathTestAssessment.Modules.Examinations.Dtos;
using MathTestAssessment.Modules.Examinations.Features;
using MathTestAssessment.Modules.Students.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathTestAssessment.Api.Controllers;

[ApiController]
[Route("api/exams")]
[Authorize(Roles = "Teacher,Student")]
public class ExamsController : ControllerBase
{
    private readonly GetExamsService _getExamsService;
    private readonly GetExamResultService _getExamResultService;
    private readonly GetStudentsService _getStudentsService;

    public ExamsController(
        GetExamsService getExamsService,
        GetExamResultService getExamResultService,
        GetStudentsService getStudentsService)
    {
        _getExamsService = getExamsService;
        _getExamResultService = getExamResultService;
        _getStudentsService = getStudentsService;
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out var id)
            ? id
            : null;

    private Guid? CurrentStudentId =>
        Guid.TryParse(
            User.FindFirstValue("StudentId"),
            out var id)
            ? id
            : null;

    private bool CanAccess(ExamDto exam)
    {

        if (User.IsInRole("Teacher"))
            return CurrentUserId.HasValue &&
                   exam.TeacherId == CurrentUserId.Value;

        if (User.IsInRole("Student"))
            return CurrentStudentId.HasValue &&
                   exam.StudentId == CurrentStudentId.Value;

        return false;
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedExamsDto>> GetPage(
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page is < 1 or > 100000 || pageSize is < 1 or > 100)
            return BadRequest(new { error = "Invalid page or page size." });

        if (User.IsInRole("Teacher") && CurrentUserId.HasValue)
        {
            var result = await _getExamsService.GetPageByTeacherAsync(
                CurrentUserId.Value, page, pageSize, cancellationToken);
            await AddStudentNamesAsync(result.Items, cancellationToken);
            return Ok(new PagedExamsDto(result.Items, result.TotalCount));
        }

        if (User.IsInRole("Student") && CurrentStudentId.HasValue)
        {
            var result = await _getExamsService.GetPageByStudentAsync(
                CurrentStudentId.Value, page, pageSize, cancellationToken);
            return Ok(new PagedExamsDto(result.Items, result.TotalCount));
        }

        return Forbid();
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        if (User.IsInRole("Teacher") && CurrentUserId.HasValue)
        {
            var statistics = await _getExamsService.GetDashboardStatsByTeacherAsync(
                CurrentUserId.Value, cancellationToken);
            var recent = await _getExamsService.GetPageByTeacherAsync(
                CurrentUserId.Value, 1, 5, cancellationToken);
            await AddStudentNamesAsync(recent.Items, cancellationToken);
            return Ok(new
            {
                statistics.TotalExams,
                statistics.TotalTasks,
                statistics.CorrectTasks,
                statistics.DistinctStudents,
                RecentExams = recent.Items
            });
        }

        if (User.IsInRole("Student") && CurrentStudentId.HasValue)
        {
            var statistics = await _getExamsService.GetDashboardStatsByStudentAsync(
                CurrentStudentId.Value, cancellationToken);
            var recent = await _getExamsService.GetPageByStudentAsync(
                CurrentStudentId.Value, 1, 5, cancellationToken);
            return Ok(new
            {
                statistics.TotalExams,
                statistics.TotalTasks,
                statistics.CorrectTasks,
                statistics.DistinctStudents,
                RecentExams = recent.Items
            });
        }

        return Forbid();
    }

    [HttpGet("student/{studentId:guid}/paged")]
    public async Task<ActionResult<PagedExamsDto>> GetStudentPage(
        Guid studentId,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page is < 1 or > 100000 || pageSize is < 1 or > 100)
            return BadRequest(new { error = "Invalid page or page size." });

        if (User.IsInRole("Teacher") && CurrentUserId.HasValue)
        {
            var result = await _getExamsService.GetPageByTeacherAndStudentAsync(
                CurrentUserId.Value, studentId, page, pageSize, cancellationToken);
            return Ok(new PagedExamsDto(result.Items, result.TotalCount));
        }

        if (User.IsInRole("Student") && CurrentStudentId == studentId)
        {
            var result = await _getExamsService.GetPageByStudentAsync(
                studentId, page, pageSize, cancellationToken);
            return Ok(new PagedExamsDto(result.Items, result.TotalCount));
        }

        return NotFound();
    }

    private async Task AddStudentNamesAsync(
        IEnumerable<ExamDto> exams,
        CancellationToken cancellationToken)
    {
        var examList = exams as IReadOnlyCollection<ExamDto> ?? exams.ToArray();
        var names = await _getStudentsService.GetNamesByIdsAsync(
            examList.Select(exam => exam.StudentId),
            cancellationToken);

        foreach (var exam in examList)
        {
            exam.StudentName = names.GetValueOrDefault(exam.StudentId, "Unknown student");
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Teacher,Student")]
    public async Task<ActionResult<ExamDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var exam = await _getExamsService.GetByIdAsync(
            id,
            cancellationToken);

        if (exam is null)
            return NotFound();

        if (!CanAccess(exam))
            return NotFound();

        return Ok(exam);
    }

    [HttpGet("{id:guid}/result")]
    [Authorize(Roles = "Teacher,Student")]
    public async Task<ActionResult<ExamResultDto>> GetResult(
        Guid id,
        CancellationToken cancellationToken)
    {
        var exam = await _getExamsService.GetByIdAsync(
            id,
            cancellationToken);

        if (exam is null || !CanAccess(exam))
            return NotFound();

        var result = await _getExamResultService.GetAsync(
            id,
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}


