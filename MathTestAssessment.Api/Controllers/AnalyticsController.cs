using System.Security.Claims;
using MathTestAssessment.Modules.Analytics.Dtos;
using MathTestAssessment.Modules.Analytics.Features;
using MathTestAssessment.Modules.Examinations.Dtos;
using MathTestAssessment.Modules.Examinations.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathTestAssessment.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Roles = "Teacher,Student")]
public class AnalyticsController : ControllerBase
{
    private readonly StudentAnalyticsService _analyticsService;
    private readonly StudentExamAnalyticsService _examAnalyticsService;
    private readonly GetExamsService _getExamsService;

    public AnalyticsController(
        StudentAnalyticsService analyticsService,
        StudentExamAnalyticsService examAnalyticsService,
        GetExamsService getExamsService)
    {
        _analyticsService = analyticsService;
        _examAnalyticsService = examAnalyticsService;
        _getExamsService = getExamsService;
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

    private bool CanAccessStudent(Guid studentId)
    {
        return 
               (User.IsInRole("Student") &&
                CurrentStudentId == studentId);
    }

    [HttpGet("students/{studentId:guid}")]
    public async Task<ActionResult<StudentAnalyticsDto>> GetStudentAnalytics(
        Guid studentId)
    {
        if (studentId == Guid.Empty)
            return BadRequest(new { error = "Student ID is required." });


        if (User.IsInRole("Teacher"))
        {
            if (!CurrentUserId.HasValue)
                return Forbid();

            var result = await _analyticsService.GetByTeacherAsync(
                studentId,
                CurrentUserId.Value);

            return Ok(result);
        }

        if (User.IsInRole("Student"))
        {
            if (CurrentStudentId != studentId)
                return NotFound();

            var result = await _analyticsService.GetAsync(studentId);
            return Ok(result);
        }

        return Forbid();
    }

    [HttpGet("students/{studentId:guid}/exams")]
    public async Task<ActionResult<PagedExamsDto>> GetStudentExams(
        Guid studentId,
        int page = 1,
        int pageSize = 10)
    {
        if (studentId == Guid.Empty)
            return BadRequest(new { error = "Student ID is required." });
        if (page is < 1 or > 100000 || pageSize is < 1 or > 100)
            return BadRequest(new { error = "Invalid page or page size." });

        if (User.IsInRole("Teacher"))
        {
            if (!CurrentUserId.HasValue)
                return Forbid();

            var teacherExams =
                await _getExamsService.GetPageByTeacherAndStudentAsync(
                    CurrentUserId.Value,
                    studentId,
                    page,
                    pageSize);

            return Ok(new PagedExamsDto(teacherExams.Items, teacherExams.TotalCount));
        }

        if (!CanAccessStudent(studentId))
            return NotFound();

        var exams = await _getExamsService.GetPageByStudentAsync(
            studentId,
            page,
            pageSize);

        return Ok(new PagedExamsDto(exams.Items, exams.TotalCount));
    }

    [HttpGet("students/{studentId:guid}/exams/{examId:guid}")]
    public async Task<ActionResult<ExamDto>> GetStudentExam(
        Guid studentId,
        Guid examId)
    {
        if (studentId == Guid.Empty || examId == Guid.Empty)
            return BadRequest(new { error = "Invalid student or exam ID." });

        var exam = await _examAnalyticsService.GetExamAsync(
            studentId,
            examId);

        if (exam is null)
            return NotFound();


        if (User.IsInRole("Teacher") &&
            CurrentUserId.HasValue &&
            exam.TeacherId == CurrentUserId.Value)
        {
            return Ok(exam);
        }

        if (User.IsInRole("Student") &&
            CurrentStudentId == exam.StudentId)
        {
            return Ok(exam);
        }

        return NotFound();
    }
}

