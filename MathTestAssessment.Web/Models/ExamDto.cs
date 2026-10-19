using System.Text;
namespace MathTestAssessment.Web.Models;
public class ExamDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid TeacherId { get; set; }
    public string ExternalExamId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public List<ExamTaskDto> Tasks { get; set; } = new();
}

