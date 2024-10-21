using Cyber_Z1.Context;
using Cyber_Z1.Models;
using Cyber_Z1.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class AccountController : Controller
{
    private readonly SecurityContext _context;
    private readonly IPasswordService _passwordService;
    private ILogService _logService;
    public AccountController(SecurityContext context, IPasswordService passwordService, ILogService logService)
    {
        _context = context;
        _passwordService = passwordService;
        _logService = logService;
    }

    // GET: Account/Login
    public IActionResult Login()
    {
        return View();
    }

    // POST: Account/Login
    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {   
    var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == username);
    if (user != null)
    {
        if (user.IsBlocked)
        {
            var remainingBlockTime = user.BlockedDate.Value - DateTime.Now;
            if (remainingBlockTime > TimeSpan.Zero)
            {
                ModelState.AddModelError("", $"Konto jest zablokowane. Pozostały czas: {remainingBlockTime.Minutes} min {remainingBlockTime.Seconds} sek.");
                return View();
            }
            else
            {
                user.IsBlocked = false;
                user.FailedLoginAttempts = 0;
                user.BlockedDate = null; 
                await _context.SaveChangesAsync();
            }
        }

        if (_passwordService.VerifyPassword(password, user.PasswordHash))
        {
            user.FailedLoginAttempts = 0;
            await _context.SaveChangesAsync();

            if (user.PasswordExpiryDate.HasValue && user.PasswordExpiryDate.Value < DateTime.Now)
            {
                TempData["RequirePasswordChange"] = true;
                HttpContext.Session.SetInt32("UserId", user.UserId);
                return RedirectToAction("ChangePassword");
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());
            await _logService.LogActivity(HttpContext.Session.GetString("Username"), "Logowanie uzytkownika",
                $"Uzytkownik '{HttpContext.Session.GetString("Username")}' zalogował się.");
            HttpContext.Session.Set("LastActivity", BitConverter.GetBytes(DateTime.Now.ToBinary()));
            return RedirectToAction("Index", "Home");
        }
        else
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= user.MaxFailedLoginAttempts)
            {
                user.IsBlocked = true;
                user.BlockedDate = DateTime.Now.AddMinutes(15); 
                ModelState.AddModelError("", "BLOKADA!!! Konto zostało zablokowane na 15 minut.");
            }
            else
            {
                ModelState.AddModelError("", "Login lub Hasło niepoprawny.");
            }

            await _context.SaveChangesAsync();
            return View();
        }
    }
    else
    {
        ModelState.AddModelError("", "Login lub Hasło niepoprawny.");
        return View();
    }
}
    // GET: Account/Logout
    public IActionResult Logout()
    {
        _logService.LogActivity(HttpContext.Session.GetString("Username"), "Wylogowanie uzytkownika", $"Uzytkownik '{HttpContext.Session.GetString("Username")} wylogowal się.");
        HttpContext.Session.Clear();
        return RedirectToAction("Login", "Account");
    }

    // GET: Account/ChangePassword
    [HttpGet]
    public async Task<IActionResult> ChangePassword()
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login");
        }

        ViewBag.RequirePasswordChange = TempData["RequirePasswordChange"] != null;

        var user = await _context.Users.FindAsync(userId.Value);

        if (user == null)
        {
            return NotFound();
        }
        HttpContext.Session.Set("LastActivity", BitConverter.GetBytes(DateTime.Now.ToBinary()));
        return View();
    }

    // POST: Account/ChangePassword
    [HttpPost]
    public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login");
        }

        var user = await _context.Users.FindAsync(userId.Value);

        if (user == null)
        {
            return NotFound();
        }

        ViewBag.RequirePasswordChange = TempData["RequirePasswordChange"] != null;

        if (!ViewBag.RequirePasswordChange && !_passwordService.VerifyPassword(oldPassword, user.PasswordHash))
        {
            ModelState.AddModelError("", "Stare hasło jest niepoprawne.");
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError("", "Nowe hasła nie są zgodne.");
            return View();
        }
        
        if (user.PasswordRestrictionsEnabled && !_passwordService.IsPasswordValid(newPassword))
        {
            ModelState.AddModelError("", "Hasło nie spełnia wymagań bezpieczeństwa. (przynajmniej jedna wielka litera, jedna cyfra, znak specjalny.)");
            return View();
        }

        if (_passwordService.IsPasswordInHistory(user, newPassword))
        {
            ModelState.AddModelError("", "Nowe hasło musi różnić się od poprzednich.");
            return View();
        }
        
        user.PasswordHash = _passwordService.HashPassword(newPassword);
        user.PasswordExpiryDate = DateTime.Now.AddDays(30);

        var passwordHistory = new PasswordHistory
        {
            UserId = user.UserId,
            PasswordHash = user.PasswordHash,
            ChangedDate = DateTime.Now
        };
        _context.PasswordHistories.Add(passwordHistory);

        await _context.SaveChangesAsync();
        await _logService.LogActivity(user.Username, "Zmiana hasła", $"Pomyslnie zmieniono haslo uzytkownika '{user.Username}'");
        HttpContext.Session.Set("LastActivity", BitConverter.GetBytes(DateTime.Now.ToBinary()));
        return RedirectToAction("Index", "Home");
    }
}
