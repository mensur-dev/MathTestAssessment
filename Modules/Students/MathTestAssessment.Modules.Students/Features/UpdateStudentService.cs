using System.Text;
using MathTestAssessment.Modules.Students.Dtos;
using MathTestAssessment.Modules.Students.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace MathTestAssessment.Modules.Students.Features;

public class UpdateStudentService
{
    private readonly StudentsDbContext _dbContext;

    public UpdateStudentService(StudentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        UpdateStudentDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ExternalStudentId) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ArgumentException("Student information is required.");
        }

        var student = await _dbContext.Students
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (student == null)
            return false;

        var externalStudentId = request.ExternalStudentId.Trim();

        var exists = await _dbContext.Students.AnyAsync(
            x => x.ExternalStudentId == externalStudentId && x.Id != id,
            cancellationToken);

        if (exists)
            throw new InvalidOperationException("This ID is already assigned to another student.");

        if (externalStudentId.Length > 100)
            throw new ArgumentException("ID cannot exceed 100 characters.");

        student.ExternalStudentId = externalStudentId;
        student.FirstName = request.FirstName.Trim();
        student.LastName = request.LastName.Trim();

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new InvalidOperationException("This ID is already assigned to another student.");
        }

        return true;
    }
}


