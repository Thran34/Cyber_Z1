using Cyber_Z1.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cyber_Z1.Context
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne<ApplicationUser>(s => s.ApplicationUser)
                .WithOne()
                .HasForeignKey<User>(s => s.ApplicationUserId);
        }

        public DbSet<User> Users { get; set; } 
    }
}