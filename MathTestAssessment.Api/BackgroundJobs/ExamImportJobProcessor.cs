using MathTestAssessment.BackgroundJobs.Configuration;
using MathTestAssessment.BackgroundJobs.Dtos;
using MathTestAssessment.BackgroundJobs.Features;
using MathTestAssessment.Modules.Imports.Features;
using Microsoft.Extensions.Options;

namespace MathTestAssessment.Api.BackgroundJobs;
public class ExamImportJobProcessor
{
    private readonly ImportExamService _importService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ImportJobSettings _settings;
    private readonly ILogger<ExamImportJobProcessor> _logger;

    public ExamImportJobProcessor(
        ImportExamService importService,
        IServiceScopeFactory scopeFactory,
        IOptions<ImportJobSettings> options,
        ILogger<ExamImportJobProcessor> logger)
    {
        _importService = importService;
        _scopeFactory = scopeFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task ProcessAsync(
        ClaimedJobDto job,
        CancellationToken stoppingToken)
    {
        using var processingCts =
            CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        using var heartbeatCts =
            CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        var heartbeatTask = RunHeartbeatAsync(
            job,
            processingCts,
            heartbeatCts.Token);

        async Task StopHeartbeatAsync()
        {
            heartbeatCts.Cancel();
            await heartbeatTask;
        }

        try
        {
            await using var stream = new FileStream(
                job.FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);

            await _importService.ImportAsync(
                stream,
                job.TeacherId,
                job.ExternalTeacherId,
                job.Id,
                processingCts.Token);

            processingCts.Token.ThrowIfCancellationRequested();

            await StopHeartbeatAsync();

            processingCts.Token.ThrowIfCancellationRequested();

            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var jobService = scope.ServiceProvider
                .GetRequiredService<ImportJobService>();

            var completed = await jobService.CompleteAsync(
                job,
                processingCts.Token);

            if (completed)
            {
                _logger.LogInformation("Exam import job {JobId} completed.", job.Id);
            }
            else
            {
                _logger.LogWarning("Could not complete exam import job {JobId}.", job.Id);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Exam import job {JobId} stopped during shutdown.", job.Id);
        }
        catch (OperationCanceledException)
            when (processingCts.IsCancellationRequested)
        {
            _logger.LogWarning("Exam import job {JobId} lost its lease.", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing exam import job {JobId}.", job.Id);

            await StopHeartbeatAsync();

            var permanentFailure =
                ex is InvalidDataException ||
                ex is ArgumentException ||
                ex is FileNotFoundException ||
                ex is DirectoryNotFoundException;

            var errorCode = permanentFailure
                ? (ex is InvalidDataException ? ex.Message[..Math.Min(ex.Message.Length, 1900)] 
                : "Invalid import data or missing XML file.")
                : "IMPORT_PROCESSING_FAILED";

            try
            {
                await using var scope =
                    _scopeFactory.CreateAsyncScope();

                var jobService = scope.ServiceProvider
                    .GetRequiredService<ImportJobService>();

                var updated = await jobService.RetryOrFailAsync(
                    job,
                    errorCode,
                    permanentFailure,
                    stoppingToken);

                if (!updated)
                {
                    _logger.LogWarning("Could not update job {JobId}; lease may be lost.", job.Id);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception updateException)
            {
                _logger.LogError(updateException, "Could not update status of job {JobId}.", job.Id);
            }
        }
        finally
        {
            heartbeatCts.Cancel();

            try
            {
                await heartbeatTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private async Task RunHeartbeatAsync(
        ClaimedJobDto job,
        CancellationTokenSource processingCts,
        CancellationToken ct)
    {
        try
        {
            using var timer =
                new PeriodicTimer(_settings.HeartbeatInterval);

            while (await timer.WaitForNextTickAsync(ct))
            {
                await using var scope =
                    _scopeFactory.CreateAsyncScope();

                var jobService = scope.ServiceProvider
                    .GetRequiredService<ImportJobService>();

                var renewed = await jobService.RenewAsync(job, ct);

                if (!renewed)
                {
                    _logger.LogWarning(
                        "Lease renewal failed for job {JobId}.",
                        job.Id);

                    processingCts.Cancel();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Heartbeat failed for job {JobId}.", job.Id);

            processingCts.Cancel();
        }
    }
}

