using MathTestAssessment.BackgroundJobs.Configuration;
using MathTestAssessment.BackgroundJobs.Data;
using MathTestAssessment.BackgroundJobs.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace MathTestAssessment.BackgroundJobs;
public static class DependencyInjection
{
    public static IServiceCollection AddBackgroundJobsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<BackgroundJobsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable(
                    "__EFMigrationsHistory_BackgroundJobs")));
        services.AddOptions<ImportJobSettings>()
                    .Bind(configuration.GetSection("ImportJobSettings"))
                    .Validate(x =>
                        x.MaxAttempts > 0 &&
                        x.MaxParallelJobs > 0 &&
                        x.LeaseDuration > TimeSpan.Zero &&
                        x.HeartbeatInterval > TimeSpan.Zero &&
                        x.HeartbeatInterval < x.LeaseDuration &&
                        x.PollInterval > TimeSpan.Zero,
                        "Invalid ImportJobSettings.")
                    .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ImportJobService>();
        return services;
    }
}
