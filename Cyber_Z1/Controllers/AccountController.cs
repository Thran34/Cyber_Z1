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
    public async Task<IActionResult> Login()
    {
        var audioQuestion = await _context.AudioQuestions.FirstOrDefaultAsync();

        if (audioQuestion != null)
        {
            ViewBag.AudioFilePath = audioQuestion.AudioFilePath.Replace("wwwroot/", "~/");
        }
        else
        {
            ViewBag.AudioFilePath = null; 
        }

        return View();
    }

    // POST: Account/Login
    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string audioAnswer)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == username);
        var audioQuestion = await _context.AudioQuestions.FirstOrDefaultAsync();

        if (user != null && audioQuestion != null)
        {
            if (_passwordService.VerifyPassword(password, user.PasswordHash) &&
                audioAnswer.Equals(audioQuestion.CorrectAnswer, StringComparison.OrdinalIgnoreCase))
            {
                user.FailedLoginAttempts = 0;
                await _context.SaveChangesAsync();

                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());
                await _logService.LogActivity(user.Username, "Login", $"User '{user.Username}' logged in.");

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Login, hasło, lub odpowiedź do pytania jest nieprawidłowa.");
                user.FailedLoginAttempts++;
                await _context.SaveChangesAsync();
                if (user.FailedLoginAttempts >= 3) 
                {
                    var canaryTokenService = HttpContext.RequestServices.GetService<ICanaryTokenService>();
                    await canaryTokenService.TriggerTokenAsync();
                }
            }
        }
        else
        {
            ModelState.AddModelError("", "Niepoprawny login lub haslo.");
        }

        return View();
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
    public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword, string gRecaptchaResponse)
    {
        var isValidCaptcha = await VerifyReCaptchaAsync(gRecaptchaResponse);
        if (!isValidCaptcha)
        {
            ModelState.AddModelError("", "reCAPTCHA validation failed.");
            return View();
        }
        
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

    private async Task<bool> VerifyReCaptchaAsync(string token)
    {
        var secret = "6LcxVWoqAAAAAGSl0Piq9O_Bw_B759mfCbmohVMs";
        using (var client = new HttpClient())
        {
            var response = await client.PostAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={token}", null);
            var result = await response.Content.ReadAsStringAsync();
            return result.Contains("\"success\": true");
        }
    }
}