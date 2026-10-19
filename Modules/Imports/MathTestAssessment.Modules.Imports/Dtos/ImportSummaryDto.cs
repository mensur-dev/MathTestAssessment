namespace MathTestAssessment.Modules.Imports.Dtos;
public class ImportSummaryDto
{
    public int StudentCount { get; set; }
    public int ExamCount { get; set; }
    public int TaskCount { get; set; }
    public List<ImportedExamSummaryDto> Exams { get; set; } = [];
}
public class ImportedExamSummaryDto
{
    public Guid ExamId { get; set; }
    public string ExternalExamId { get; set; } = "";
    public string ExternalStudentId { get; set; } = "";
    public int TaskCount { get; set; }
    public int CorrectTasks { get; set; }
    public int IncorrectTasks { get; set; }
    public int UnevaluatedTasks { get; set; }
}
