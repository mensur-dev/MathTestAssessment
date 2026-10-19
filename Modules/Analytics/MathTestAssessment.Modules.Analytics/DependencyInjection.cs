using System.Text;
using MathTestAssessment.Modules.Analytics.Features;
using Microsoft.Extensions.DependencyInjection;

namespace MathTestAssessment.Modules.Analytics;
public static class DependencyInjection
{
    public static IServiceCollection AddAnalyticsModule(
        this IServiceCollection services)
    {
        services.AddScoped<StudentAnalyticsService>();
        services.AddScoped<StudentExamAnalyticsService>();

        return services;
    }
}
