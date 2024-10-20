namespace Cyber_Z1.Services.Abstract;

public interface ILogService
{
    Task LogActivity(string username, string action, string description);
}