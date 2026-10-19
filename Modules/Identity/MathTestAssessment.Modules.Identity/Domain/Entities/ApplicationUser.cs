using System.Text;
using Microsoft.AspNetCore.Identity;

namespace MathTestAssessment.Modules.Identity.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public string? ExternalTeacherId { get; set; }

    public Guid? StudentId { get; set; }

    public bool MustChangePassword { get; set; }
}

