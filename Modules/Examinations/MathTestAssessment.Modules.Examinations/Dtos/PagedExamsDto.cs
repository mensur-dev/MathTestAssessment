namespace MathTestAssessment.Modules.Examinations.Dtos;

public sealed record PagedExamsDto(List<ExamDto> Items, int TotalCount);
