using Cyber_Z1.Context;

namespace Cyber_Z1;

public class InactivityBlockMiddleware
{
    private readonly RequestDelegate _next;

    public InactivityBlockMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, SecurityContext dbContext)
    {
        var userId = context.Session.GetInt32("UserId");
        if (userId.HasValue)
        {
            var user = await dbContext.Users.FindAsync(userId.Value);
            if (user != null)
            {
                if (!user.IsBlocked && context.Session.TryGetValue("LastActivity", out var lastActivityBytes))
                {
                    var lastActivity = DateTime.FromBinary(BitConverter.ToInt64(lastActivityBytes));
                    if (DateTime.Now - lastActivity > TimeSpan.FromMinutes(1))
                    {
                        user.IsBlocked = true;
                        user.BlockedDate = DateTime.Now.AddMinutes(1);
                        await dbContext.SaveChangesAsync();

                        context.Session.Clear();
                        context.Response.Redirect("/Account/Login");
                        return;
                    }
                }
                else
                {
                    context.Session.Set("LastActivity", BitConverter.GetBytes(DateTime.Now.ToBinary()));
                }
            }
        }

        await _next(context);
    }
}
