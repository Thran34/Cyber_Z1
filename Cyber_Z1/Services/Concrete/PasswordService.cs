using Cyber_Z1.Context;
using Cyber_Z1.Models;
using Cyber_Z1.Services.Abstract;

namespace Cyber_Z1.Services.Concrete
{
    public class PasswordService : IPasswordService
    {
        private readonly SecurityContext _context;

        public PasswordService(SecurityContext context)
        {
            _context = context;
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
        
        public bool IsPasswordValid(string password)
        {
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            return hasUpper && hasLower && hasSpecial;
        }

        public bool IsPasswordInHistory(User user, string newPassword)
        {
            var passwordHistories = _context.PasswordHistories
                .Where(ph => ph.UserId == user.UserId)
                .OrderByDescending(ph => ph.ChangedDate)
                .Take(5)
                .ToList();

            foreach (var history in passwordHistories)
            {
                if (VerifyPassword(newPassword, history.PasswordHash))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
