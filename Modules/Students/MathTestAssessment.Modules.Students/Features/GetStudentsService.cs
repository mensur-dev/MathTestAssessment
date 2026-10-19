using System.Text;
using MathTestAssessment.Modules.Students.Dtos;
using MathTestAssessment.Modules.Students.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MathTestAssessment.Modules.Students.Features;

public class GetStudentsService
{
    private readonly StudentsDbContext _dbContext;

    public GetStudentsService(StudentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StudentDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Students
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new StudentDto
            {
                Id = x.Id,
                ExternalStudentId = x.ExternalStudentId,
                FirstName = x.FirstName,
                LastName = x.LastName
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, string>> GetNamesByIdsAsync(
        IEnumerable<Guid> studentIds,
        CancellationToken cancellationToken = default)
    {
        var ids = studentIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        return await _dbContext.Students
            .AsNoTracking()
            .Where(student => ids.Contains(student.Id))
            .Select(student => new
            {
                student.Id,
                FullName = (student.FirstName + " " + student.LastName).Trim()
            })
            .ToDictionaryAsync(
                student => student.Id,
                student => student.FullName,
                cancellationToken);
    }

    public Task<bool> ExternalCodeExistsAsync(
        string externalStudentId,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = externalStudentId.Trim();
        return _dbContext.Students
            .AsNoTracking()
            .AnyAsync(student => student.ExternalStudentId == normalizedCode, cancellationToken);
    }

    public async Task<Dictionary<string, Guid>> GetIdsByExternalCodesAsync(
        IEnumerable<string> externalStudentIds,
        CancellationToken cancellationToken = default)
    {
        var codes = externalStudentIds
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (codes.Length == 0)
        {
            return new Dictionary<string, Guid>(StringComparer.Ordinal);
        }

        var matches = await _dbContext.Students
            .AsNoTracking()
            .Where(student => codes.Contains(student.ExternalStudentId))
            .Select(student => new { student.ExternalStudentId, student.Id })
            .ToListAsync(cancellationToken);

        return matches.ToDictionary(
            student => student.ExternalStudentId,
            student => student.Id,
            StringComparer.Ordinal);
    }

    public Task<List<StudentDto>> SearchAsync(
        string? search,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Students.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(student =>
                student.ExternalStudentId.Contains(term)
                || student.FirstName.Contains(term)
                || student.LastName.Contains(term));
        }

        return query
            .OrderBy(student => student.FirstName)
            .ThenBy(student => student.LastName)
            .ThenBy(student => student.Id)
            .Take(take)
            .Select(student => new StudentDto
            {
                Id = student.Id,
                ExternalStudentId = student.ExternalStudentId,
                FirstName = student.FirstName,
                LastName = student.LastName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<StudentDto> Items, int TotalCount)> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Students.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(student => student.FirstName)
            .ThenBy(student => student.LastName)
            .ThenBy(student => student.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(student => new StudentDto
            {
                Id = student.Id,
                ExternalStudentId = student.ExternalStudentId,
                FirstName = student.FirstName,
                LastName = student.LastName
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}


