using Cyber_Z1.Models;

namespace Cyber_Z1.Services.Abstract
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string storedHash);
        bool IsPasswordValid(string password);
        bool IsPasswordInHistory(User user, string newPassword);
    }
}