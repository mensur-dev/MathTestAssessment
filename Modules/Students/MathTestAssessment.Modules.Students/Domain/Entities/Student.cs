using System.Text;


namespace MathTestAssessment.Modules.Students.Domain.Entities;

public class Student
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ExternalStudentId { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
}

