using System.Text;


namespace MathTestAssessment.Modules.Examinations.Domain.Entities;

public class Exam
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudentId { get; set; }

    public Guid TeacherId { get; set; }

    public string ExternalExamId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ExamTask> Tasks { get; set; } = new();
}

