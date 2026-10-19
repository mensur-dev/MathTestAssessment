using System.Security.Claims;
using MathTestAssessment.BackgroundJobs.Dtos;
using MathTestAssessment.BackgroundJobs.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MathTestAssessment.Modules.Examinations.Infrastructure.Data;
using MathTestAssessment.Modules.Imports.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MathTestAssessment.Api.Controllers;

[ApiController]
[Route("api/import-jobs")]
[Authorize(Roles = "Teacher")]
public class ImportJobsController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    private readonly ImportJobService _jobService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ImportJobsController> _logger;

    public ImportJobsController(
        ImportJobService jobService,
        IWebHostEnvironment environment,
        ILogger<ImportJobsController> logger)
    {
        _jobService = jobService;
        _environment = environment;
        _logger = logger;
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "XML file is required." });

        if (file.Length > MaxFileSize)
            return BadRequest(new { error = "XML file is too large." });

        if (!string.Equals(
            Path.GetExtension(file.FileName),
            ".xml",
            StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only XML files are allowed." });
        }

        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var externalTeacherId = User.FindFirstValue(
            "ExternalTeacherId");

        if (!Guid.TryParse(userId, out var teacherId) ||
            teacherId == Guid.Empty ||
            string.IsNullOrWhiteSpace(externalTeacherId))
        {
            return Forbid();
        }

        var directory = Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            "ImportJobs");

        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(
            directory,
            $"{Guid.NewGuid():N}.xml");

        try
        {
            await using (var destination = new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true))
            {
                await file.CopyToAsync(
                    destination,
                    cancellationToken);
            }

            var jobId = await _jobService.CreateAsync(
                new CreateImportJobDto
                {
                    TeacherId = teacherId,
                    ExternalTeacherId = externalTeacherId,
                    FilePath = filePath,
                    FileName = Path.GetFileName(file.FileName).Length <= 255
                        ? Path.GetFileName(file.FileName)
                        : Path.GetFileName(file.FileName)[..255]
                },
                cancellationToken);

            return AcceptedAtAction(
                nameof(GetById),
                new { id = jobId },
                new { id = jobId, status = "Pending" });
        }
        catch
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to clean up uploaded import file.");
            }

            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var teacherId)
            || teacherId == Guid.Empty) return Forbid();
        if (page is < 1 or > 100000 || pageSize is < 1 or > 100)
            return BadRequest(new { error = "Invalid page or page size." });
        var result = await _jobService.ListForTeacherAsync(teacherId, page, pageSize, ct);
        return Ok(new { result.Items, result.TotalCount });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var teacherId) ||
            teacherId == Guid.Empty)
        {
            return Forbid();
        }

        var job = await _jobService.GetByIdAsync(
            id,
            teacherId,
            cancellationToken);

        if (job == null)
            return NotFound();

        return Ok(job);
    }

    [HttpGet("{id:guid}/summary")]
    public async Task<IActionResult> Summary(Guid id, [FromServices] ExaminationsDbContext exams, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var teacherId)) return Forbid();
        // Ownership must be checked before reading the persisted summary.
        if (await _jobService.GetByIdAsync(id, teacherId, ct) is null) return NotFound();
        var json = await exams.ProcessedImports.AsNoTracking().Where(x => x.ImportJobId == id)
            .Select(x => x.SummaryJson).FirstOrDefaultAsync(ct);
        if (json is null) return NoContent();
        return Ok(JsonSerializer.Deserialize<ImportSummaryDto>(json));
    }
}
