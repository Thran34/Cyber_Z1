using Cyber_Z1.Context;
using Cyber_Z1.Models;
using Cyber_Z1.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class AccountController : Controller
{
    private readonly SecurityContext _context;
    private readonly IPasswordService _passwordService;

    public AccountController(SecurityContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
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

        if (user != null && _passwordService.VerifyPassword(password, user.PasswordHash))
        {
            if (user.BlockedDate.HasValue && user.BlockedDate.Value <= DateTime.Now)
            {
                user.IsBlocked = true;
                await _context.SaveChangesAsync();
                ModelState.AddModelError("", "Konto jest zablokowane.");
                return View();
            }

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

            return RedirectToAction("Index", "Home");
        }
        else
        {
            ModelState.AddModelError("", "Login lub Hasło niepoprawny");
            return View();
        }
    }
    
    // GET: Account/Logout
    public IActionResult Logout()
    {
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

        return RedirectToAction("Index", "Home");
    }
}
