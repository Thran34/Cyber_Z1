using Microsoft.AspNetCore.Identity;

namespace Cyber_Z1.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}