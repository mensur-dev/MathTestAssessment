using System.Text;
namespace MathTestAssessment.BackgroundJobs.Dtos;
public class CreateImportJobDto
{
    public Guid TeacherId { get; set; }
    public string ExternalTeacherId { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string? FileName { get; set; }
}
