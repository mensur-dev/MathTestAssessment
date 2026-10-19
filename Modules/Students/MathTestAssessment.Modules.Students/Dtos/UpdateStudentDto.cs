using System.Text;
namespace MathTestAssessment.Modules.Students.Dtos;
public class UpdateStudentDto
{
    public string ExternalStudentId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
