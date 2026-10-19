using MathTestAssessment.BackgroundJobs.Data;
using MathTestAssessment.Modules.Examinations.Infrastructure.Data;
using MathTestAssessment.Modules.Identity.Data;
using MathTestAssessment.Modules.Identity.Features;
using MathTestAssessment.Modules.Students.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MathTestAssessment.Api;

public static class DatabaseInitialization
{
    public static async Task InitializeDatabaseAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseInitialization");

        logger.LogInformation("Applying database migrations.");

        await MigrateAsync<IdentityDbContext>(services, cancellationToken);
        await MigrateAsync<StudentsDbContext>(services, cancellationToken);
        await MigrateAsync<ExaminationsDbContext>(services, cancellationToken);
        await MigrateAsync<BackgroundJobsDbContext>(services, cancellationToken);

        logger.LogInformation("Database migrations applied successfully.");

        var seeder = services.GetRequiredService<AdminSeeder>();
        await seeder.SeedAsync();

        logger.LogInformation("Roles and initial Admin account are ready.");
    }

    private static async Task MigrateAsync<TContext>(
        IServiceProvider services,
        CancellationToken cancellationToken)
        where TContext : DbContext
    {
        var context = services.GetRequiredService<TContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
