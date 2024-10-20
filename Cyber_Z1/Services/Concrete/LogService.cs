using Cyber_Z1.Context;
using Cyber_Z1.Models;
using Cyber_Z1.Services.Abstract;

namespace Cyber_Z1.Services.Concrete;

public class LogService : ILogService
{
    private readonly SecurityContext _context;

    public LogService(SecurityContext context)
    {
        _context = context;
    }
    
    public async Task LogActivity(string username, string action, string description)
    {
        var log = new UserActivityLog
        {
            Username = username,
            Timestamp = DateTime.Now,
            Action = action,
            Description = description
        };

        _context.UserActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}