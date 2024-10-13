using Cyber_Z1.Models;
using Microsoft.EntityFrameworkCore;

namespace Cyber_Z1.Context
{
    public class SecurityContext : DbContext
    {
        public SecurityContext(DbContextOptions<SecurityContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<PasswordHistory> PasswordHistories { get; set; }
    }
}