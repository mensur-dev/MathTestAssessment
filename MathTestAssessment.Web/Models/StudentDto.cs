using System.Text;
namespace MathTestAssessment.Web.Models;
public class StudentDto
{
    public Guid Id { get; set; }
    public string ExternalStudentId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName => $"{FirstName} {LastName} · {ExternalStudentId}";
}

