using MathTestAssessment.Api.BackgroundJobs;

namespace MathTestAssessment.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiBackgroundJobs(
        this IServiceCollection services)
    {
        services.AddScoped<ExamImportJobProcessor>();
        services.AddHostedService<ExamImportBackgroundService>();

        return services;
    }
}