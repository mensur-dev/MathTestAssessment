using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;

namespace MathTestAssessment.Api.Infrastructure;

// Protect the cross-module check AND write, including student ID updates.
// One database-scoped resource avoids mismatches with SQL collation (case/accents).
// A database lock also coordinates separate API instances, unlike SemaphoreSlim.
[AttributeUsage(AttributeTargets.Method)]
public sealed class SerializeExternalIdWritesAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var configuration = context.HttpContext.RequestServices
            .GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("DefaultConnection must be configured.");

        var cancellationToken = context.HttpContext.RequestAborted;
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = N'MathTestAssessment.ExternalIdWrites',
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;
            SELECT @result;
            """;

        int result;
        try
        {
            result = (int)(await command.ExecuteScalarAsync(cancellationToken))!;
        }
        catch (SqlException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException("ID update was cancelled while waiting.", ex, cancellationToken);
        }
        if (result < 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.Result = new ObjectResult(new
            {
                error = "Another ID update is in progress. Please try again."
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            context.HttpContext.Response.Headers.RetryAfter = "1";
            return;
        }

        // The action's DbContexts commit their own writes before releasing this lock.
        // Disposing this otherwise empty transaction releases the lock on every exit,
        // including validation failures, exceptions and request cancellation.
        await next();
    }
}
