using MathTestAssessment.BackgroundJobs.Configuration;
using MathTestAssessment.BackgroundJobs.Features;
using Microsoft.Extensions.Options;
using MathTestAssessment.Modules.Examinations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MathTestAssessment.Api.BackgroundJobs;

public class ExamImportBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<ExamImportBackgroundService> _logger;
    private readonly ImportJobSettings _settings;

    public ExamImportBackgroundService(
        IServiceScopeFactory scopes,
        ILogger<ExamImportBackgroundService> logger,
        IOptions<ImportJobSettings> options)
    {
        _scopes = scopes;
        _logger = logger;
        _settings = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var workers = Enumerable
            .Range(0, _settings.MaxParallelJobs)
            .Select(_ => RunWorkerAsync(stoppingToken))
            .ToList();

        workers.Add(RunRecoveryAsync(stoppingToken));

        await Task.WhenAll(workers);
    }

    private async Task RunWorkerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();

                var jobService = scope.ServiceProvider
                    .GetRequiredService<ImportJobService>();

                var job = await jobService.TryClaimAsync(ct);

                if (job is null)
                {
                    await Task.Delay(_settings.PollInterval, ct);
                    continue;
                }

                var processor = scope.ServiceProvider
                    .GetRequiredService<ExamImportJobProcessor>();

                await processor.ProcessAsync(job, ct);
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in exam import worker.");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
                catch (OperationCanceledException)
                    when (ct.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task RunRecoveryAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();

                var jobService = scope.ServiceProvider
                    .GetRequiredService<ImportJobService>();

                await jobService.MarkExhaustedAsync(ct);

                var jobIds =
                    await jobService.GetJobsNeedingReviewAsync(ct);

                var examinationsDb = scope.ServiceProvider
                    .GetRequiredService<ExaminationsDbContext>();

                foreach (var jobId in jobIds)
                {
                    ct.ThrowIfCancellationRequested();

                    var completed = await examinationsDb
                        .ProcessedImports
                        .AsNoTracking()
                        .AnyAsync(
                            x => x.ImportJobId == jobId,
                            ct);

                    await jobService.ResolveReviewAsync(
                        jobId,
                        completed,
                        ct);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during exam import job recovery.");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
                catch (OperationCanceledException)
                    when (ct.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
