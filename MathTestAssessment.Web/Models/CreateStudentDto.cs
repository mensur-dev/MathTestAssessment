using System.Text;
namespace MathTestAssessment.Web.Models;
public class CreateStudentDto
{
    public string ExternalStudentId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}


