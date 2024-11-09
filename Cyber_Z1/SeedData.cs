using Cyber_Z1.Context;
using Cyber_Z1.Models;
using Cyber_Z1.Services.Abstract;
using Microsoft.EntityFrameworkCore;

namespace Cyber_Z1;

public static class SeedData
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using (var context = new SecurityContext(
                   serviceProvider.GetRequiredService<DbContextOptions<SecurityContext>>()))
        {
            if (context.Users.Any())
            {
                return; 
            }

            var passwordService = serviceProvider.GetRequiredService<IPasswordService>();

            var adminUser = new User
            {
                Username = "ADMIN",
                FullName = "Administrator",
                IsAdmin = true,
                IsBlocked = false,
                BlockedDate = DateTime.Now.AddDays(30),
                PasswordRestrictionsEnabled = true,
                PasswordExpiryDate = DateTime.Now.AddDays(30),
                PasswordHash = passwordService.HashPassword("123")
            };
            var startupQuestion = new AudioQuestion
            {
                AudioFilePath = "wwwroot/audio/Wskaz_Zwierze.mp3",
                CorrectAnswer = "pies"
            };
            context.AudioQuestions.Add(startupQuestion);
            context.Users.Add(adminUser);
            context.SaveChanges();
        }
    }
}