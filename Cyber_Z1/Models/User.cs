namespace Cyber_Z1.Models;

public class User
{
    public int UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }
    public string Role { get; set; }
    public ApplicationUser ApplicationUser { get; set; }
    public string ApplicationUserId { get; set; }
}