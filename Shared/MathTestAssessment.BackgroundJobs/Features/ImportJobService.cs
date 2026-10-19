using MathTestAssessment.BackgroundJobs.Configuration;
using MathTestAssessment.BackgroundJobs.Data;
using MathTestAssessment.BackgroundJobs.Domain.Entities;
using MathTestAssessment.BackgroundJobs.Domain.Enums;
using MathTestAssessment.BackgroundJobs.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MathTestAssessment.BackgroundJobs.Features;

public class ImportJobService
{
    private readonly BackgroundJobsDbContext _db;
    private readonly TimeProvider _clock;
    private readonly ImportJobSettings _settings;

    public ImportJobService(
        BackgroundJobsDbContext db,
        TimeProvider clock,
        IOptions<ImportJobSettings> options)
    {
        _db = db;
        _clock = clock;
        _settings = options.Value;
    }

    public async Task<Guid> CreateAsync(
        CreateImportJobDto dto,
        CancellationToken ct = default)
    {
        if (dto.TeacherId == Guid.Empty ||
            string.IsNullOrWhiteSpace(dto.ExternalTeacherId) ||
            string.IsNullOrWhiteSpace(dto.FilePath))
        {
            throw new ArgumentException("Invalid import job.");
        }

        var now = _clock.GetUtcNow().UtcDateTime;

        var job = new ImportJob
        {
            TeacherId = dto.TeacherId,
            ExternalTeacherId = dto.ExternalTeacherId,
            FilePath = dto.FilePath,
            FileName = dto.FileName,
            CreatedAtUtc = now,
            AvailableAtUtc = now
        };

        _db.ImportJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        return job.Id;
    }

    public Task<ImportJobDto?> GetByIdAsync(
        Guid jobId,
        Guid teacherId,
        CancellationToken ct = default)
    {
        return _db.ImportJobs
            .AsNoTracking()
            .Where(x => x.Id == jobId &&
                        x.TeacherId == teacherId)
            .Select(x => new ImportJobDto
            {
                Id = x.Id,
                FileName = x.FileName,
                Status = x.Status,
                AttemptCount = x.AttemptCount,
                CreatedAtUtc = x.CreatedAtUtc,
                StartedAtUtc = x.StartedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                ErrorMessage = x.ErrorMessage
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ClaimedJobDto?> TryClaimAsync(
        CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var candidates = await _db.ImportJobs
            .AsNoTracking()
            .Where(x =>
                x.AttemptCount < _settings.MaxAttempts &&
                (
                    (x.Status == ImportJobStatus.Pending &&
                     x.AvailableAtUtc <= now)
                    ||
                    (x.Status == ImportJobStatus.Processing &&
                     x.LeaseExpiresAtUtc <= now)
                ))
            .OrderBy(x => x.AvailableAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.AttemptCount
            })
            .Take(8)
            .ToListAsync(ct);

        foreach (var candidate in candidates)
        {
            var token = Guid.NewGuid();
            var expiresAt = now.Add(_settings.LeaseDuration);

            var updated = await _db.ImportJobs
                .Where(x =>
                    x.Id == candidate.Id &&
                    x.AttemptCount == candidate.AttemptCount &&
                    x.AttemptCount < _settings.MaxAttempts &&
                    (
                        (x.Status == ImportJobStatus.Pending &&
                         x.AvailableAtUtc <= now)
                        ||
                        (x.Status == ImportJobStatus.Processing &&
                         x.LeaseExpiresAtUtc <= now)
                    ))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status,
                        ImportJobStatus.Processing)
                    .SetProperty(x => x.AttemptCount,
                        x => x.AttemptCount + 1)
                    .SetProperty(x => x.LeaseToken,
                        (Guid?)token)
                    .SetProperty(x => x.LeaseExpiresAtUtc,
                        (DateTime?)expiresAt)
                    .SetProperty(x => x.StartedAtUtc,
                        (DateTime?)now),
                    ct);

            if (updated != 1)
                continue;

            return await _db.ImportJobs
                .AsNoTracking()
                .Where(x => x.Id == candidate.Id &&
                            x.LeaseToken == token)
                .Select(x => new ClaimedJobDto
                {
                    Id = x.Id,
                    TeacherId = x.TeacherId,
                    ExternalTeacherId = x.ExternalTeacherId,
                    FilePath = x.FilePath,
                    LeaseToken = token,
                    AttemptCount = x.AttemptCount
                })
                .SingleAsync(ct);
        }

        return null;
    }

    public async Task<bool> RenewAsync(
        ClaimedJobDto job,
        CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var updated = await OwnedJob(job, now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.LeaseExpiresAtUtc,
                    (DateTime?)now.Add(_settings.LeaseDuration)),
                ct);

        return updated == 1;
    }

    public async Task<bool> CompleteAsync(
        ClaimedJobDto job,
        CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var updated = await OwnedJob(job, now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status,
                    ImportJobStatus.Completed)
                .SetProperty(x => x.CompletedAtUtc,
                    (DateTime?)now)
                .SetProperty(x => x.LeaseToken,
                    (Guid?)null)
                .SetProperty(x => x.LeaseExpiresAtUtc,
                    (DateTime?)null)
                .SetProperty(x => x.ErrorMessage,
                    (string?)null),
                ct);

        return updated == 1;
    }

    public async Task<bool> RetryOrFailAsync(
        ClaimedJobDto job,
        string errorCode,
        bool permanentFailure,
        CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var exhausted =
            job.AttemptCount >= _settings.MaxAttempts;

        var delaySeconds =
            Math.Min(300, Math.Pow(2, job.AttemptCount) * 5) +
            Random.Shared.Next(0, 5);

        var status = permanentFailure
            ? ImportJobStatus.Failed
            : exhausted
                ? ImportJobStatus.NeedsReview
                : ImportJobStatus.Pending;

        var updated = await OwnedJob(job, now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.AvailableAtUtc,
                    now.AddSeconds(delaySeconds))
                .SetProperty(x => x.LeaseToken,
                    (Guid?)null)
                .SetProperty(x => x.LeaseExpiresAtUtc,
                    (DateTime?)null)
                .SetProperty(x => x.ErrorMessage, errorCode),
                ct);

        return updated == 1;
    }

    public Task<int> MarkExhaustedAsync(
        CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        return _db.ImportJobs
            .Where(x =>
                x.AttemptCount >= _settings.MaxAttempts &&
                (
                    x.Status == ImportJobStatus.Pending ||
                    (x.Status == ImportJobStatus.Processing &&
                     x.LeaseExpiresAtUtc <= now)
                ))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status,
                    ImportJobStatus.NeedsReview)
                .SetProperty(x => x.LeaseToken,
                    (Guid?)null)
                .SetProperty(x => x.LeaseExpiresAtUtc,
                    (DateTime?)null)
                .SetProperty(x => x.ErrorMessage,
                    "Import_Requires_Review"),
                ct);
    }

    public Task<List<Guid>> GetJobsNeedingReviewAsync(
        CancellationToken ct = default)
    {
        return _db.ImportJobs
            .AsNoTracking()
            .Where(x => x.Status == ImportJobStatus.NeedsReview)
            .OrderBy(x => x.AvailableAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .Take(100)
            .ToListAsync(ct);
    }

    public async Task<bool> ResolveReviewAsync(
        Guid jobId,
        bool importCompleted,
        CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;

        var status = importCompleted
            ? ImportJobStatus.Completed
            : ImportJobStatus.Failed;

        var updated = await _db.ImportJobs
            .Where(x =>
                x.Id == jobId &&
                x.Status == ImportJobStatus.NeedsReview)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.CompletedAtUtc,
                    importCompleted ? (DateTime?)now : null)
                .SetProperty(x => x.ErrorMessage,
                    importCompleted
                        ? (string?)null
                        : "Import_Failed_After_Max_Attempts"),
                ct);

        return updated == 1;
    }

    private IQueryable<ImportJob> OwnedJob(
        ClaimedJobDto job,
        DateTime now)
    {
        return _db.ImportJobs.Where(x =>
            x.Id == job.Id &&
            x.Status == ImportJobStatus.Processing &&
            x.LeaseToken == job.LeaseToken &&
            x.LeaseExpiresAtUtc > now);
    }

    public async Task<(List<ImportJobDto> Items, int TotalCount)> ListForAdminAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = _db.ImportJobs.AsNoTracking();
        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ImportJobDto {
                Id = x.Id, Status = x.Status, AttemptCount = x.AttemptCount,
                CreatedAtUtc = x.CreatedAtUtc, StartedAtUtc = x.StartedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc, ErrorMessage = x.ErrorMessage
            }).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<(List<ImportJobDto> Items, int TotalCount)> ListForTeacherAsync(
        Guid teacherId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ImportJobs.AsNoTracking().Where(job => job.TeacherId == teacherId);
        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(job => job.CreatedAtUtc).ThenBy(job => job.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(job => new ImportJobDto
            {
                Id = job.Id, FileName = job.FileName, Status = job.Status,
                AttemptCount = job.AttemptCount, CreatedAtUtc = job.CreatedAtUtc,
                StartedAtUtc = job.StartedAtUtc, CompletedAtUtc = job.CompletedAtUtc,
                ErrorMessage = job.ErrorMessage
            }).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<bool> RestartFailedAsync(Guid id, CancellationToken ct)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        
        return await _db.ImportJobs.Where(x => x.Id == id && x.Status == ImportJobStatus.Failed)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, ImportJobStatus.Pending)
                .SetProperty(x => x.AttemptCount, 0)
                .SetProperty(x => x.AvailableAtUtc, now)
                .SetProperty(x => x.StartedAtUtc, (DateTime?)null)
                .SetProperty(x => x.CompletedAtUtc, (DateTime?)null)
                .SetProperty(x => x.LeaseToken, (Guid?)null)
                .SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null)
                .SetProperty(x => x.ErrorMessage, (string?)null), ct) == 1;
    }

}

