namespace Cyber_Z1.Models;

public class UserActivityLog
{
    public int Id { get; set; }
    public string Username { get; set; }
    public DateTime Timestamp { get; set; }
    public string Action { get; set; }
    public string Description { get; set; } 
}