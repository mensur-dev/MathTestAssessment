using System.Text;
using MathTestAssessment.MathEngine.Evaluation;
using MathTestAssessment.Modules.Examinations.Features;
using MathTestAssessment.Modules.Examinations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MathTestAssessment.Modules.Examinations;
public static class DependencyInjection
{
    public static IServiceCollection AddExaminationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(
            "DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing.");

        services.AddDbContext<ExaminationsDbContext>(options =>
                 options.UseSqlServer(connectionString, sqlOptions =>
                 sqlOptions.MigrationsHistoryTable(
                 "__EFMigrationsHistory_Examinations",
                 "dbo")));

        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();

        services.AddScoped<EvaluateExamService>();
        services.AddScoped<GetExamsService>();
        services.AddScoped<GetExamResultService>();

        return services;
    }
}


