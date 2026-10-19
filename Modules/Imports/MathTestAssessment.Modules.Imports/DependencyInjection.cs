using MathTestAssessment.Modules.Examinations.Features;
using MathTestAssessment.Modules.Imports.Features;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace MathTestAssessment.Modules.Imports;

public static class DependencyInjection
{
    public static IServiceCollection AddImportsModule(
        this IServiceCollection services)
    {
        services.AddScoped<XmlExamParser>();
        services.AddScoped<ImportExamService>();

        return services;
    }
}

