namespace MathTestAssessment.Web.Models;
public class UserSummary
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public List<string> Roles { get; set; } = [];
    public string RoleDisplay => string.Join(", ", Roles);
}

