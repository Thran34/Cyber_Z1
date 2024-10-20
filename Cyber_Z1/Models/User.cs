using System.ComponentModel.DataAnnotations;

namespace Cyber_Z1.Models;

public class User
{
    [Key]
    public int UserId { get; set; }
    [Required]
    [Display(Name = "Nazwa Użytkownika")]
    public string Username { get; set; } 
    [Required]
    [Display(Name = "Pełna Nazwa")]
    public string FullName { get; set; }
    [Required]
    public string PasswordHash { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsBlocked { get; set; }
    public bool PasswordRestrictionsEnabled { get; set; }
    public DateTime? PasswordExpiryDate { get; set; }
    public DateTime? BlockedDate { get; set; } 
    public int FailedLoginAttempts { get; set; }
    public virtual ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();
}