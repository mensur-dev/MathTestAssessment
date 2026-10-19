using System.Text;
using MathTestAssessment.Modules.Students.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MathTestAssessment.Modules.Students.Features;

namespace MathTestAssessment.Modules.Students;

public static class DependencyInjection
{
    public static IServiceCollection AddStudentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<StudentsDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable(
                    "__EFMigrationsHistory_Students", "dbo")));

        services.AddScoped<CreateStudentService>();
        services.AddScoped<GetStudentsService>();
        services.AddScoped<UpdateStudentService>();

        return services;
    }
}
