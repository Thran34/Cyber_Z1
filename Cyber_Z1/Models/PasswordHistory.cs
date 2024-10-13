using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cyber_Z1.Models
{
    public class PasswordHistory
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public string PasswordHash { get; set; }

        public DateTime ChangedDate { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}