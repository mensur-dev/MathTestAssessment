using MathTestAssessment.Modules.Students.Domain.Entities;
using MathTestAssessment.Modules.Students.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using MathTestAssessment.Modules.Students.Dtos;
using Microsoft.Data.SqlClient;

namespace MathTestAssessment.Modules.Students.Features;

public class CreateStudentService
{
    private readonly StudentsDbContext _dbContext;

    public CreateStudentService(StudentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> CreateAsync(
        CreateStudentDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ExternalStudentId) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ArgumentException("Student information is required.");
        }

        var externalStudentId = request.ExternalStudentId.Trim();

        var exists = await _dbContext.Students.AnyAsync(
            x => x.ExternalStudentId == externalStudentId,
            cancellationToken);

        if (exists)
            throw new InvalidOperationException("This ID is already assigned to another student.");

        if (externalStudentId.Length > 100)
            throw new ArgumentException("ID cannot exceed 100 characters.");

        var student = new Student
        {
            ExternalStudentId = externalStudentId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim()
        };

        _dbContext.Students.Add(student);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new InvalidOperationException("This ID is already assigned to another student.");
        }

        return student.Id;
    }
}


