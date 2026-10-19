using MathTestAssessment.Api.Infrastructure;
using MathTestAssessment.Modules.Students.Dtos;
using MathTestAssessment.Modules.Students.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MathTestAssessment.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MathTestAssessment.Api.Controllers;

[ApiController]
[Route("api/students")]
[Authorize(Roles = "Teacher")]
public class StudentsController : ControllerBase
{
    private readonly CreateStudentService _createService;
    private readonly GetStudentsService _getService;
    private readonly UpdateStudentService _updateService;
    private readonly UserManager<ApplicationUser> _users;

    public StudentsController(
        CreateStudentService createService,
        GetStudentsService getService,
        UpdateStudentService updateService,
        UserManager<ApplicationUser> users)
    {
        _createService = createService;
        _getService = getService;
        _updateService = updateService;
        _users = users;
    }

    [HttpPost]
    [SerializeExternalIdWrites]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Create(
        [FromBody] CreateStudentDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (await CodeBelongsToTeacher(request.ExternalStudentId, cancellationToken))
                return Conflict(new { error = "This ID is already assigned to a teacher." });

            var id = await _createService.CreateAsync(
                request, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id },
                new { id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("paged")]
    public async Task<IActionResult> GetPage(
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            return BadRequest(new { error = "Invalid page or page size." });

        var result = await _getService.GetPageAsync(page, pageSize, cancellationToken);
        return Ok(new { result.Items, result.TotalCount });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        string? search,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _getService.SearchAsync(search, 100, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<StudentDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var student = await _getService.GetByIdAsync(
            id, cancellationToken);

        if (student is null)
            return NotFound();

        return Ok(student);
    }

    [HttpPut("{id:guid}")]
    [SerializeExternalIdWrites]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateStudentDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (await CodeBelongsToTeacher(request.ExternalStudentId, cancellationToken))
                return Conflict(new { error = "This ID is already assigned to a teacher." });

            var updated = await _updateService.UpdateAsync(
                id, request, cancellationToken);

            if (!updated)
                return NotFound();

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    private Task<bool> CodeBelongsToTeacher(string? code, CancellationToken cancellationToken)
    {
        var normalized = code?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return Task.FromResult(false);
        return _users.Users.AsNoTracking().AnyAsync(
            x => x.ExternalTeacherId == normalized,
            cancellationToken);
    }
}


