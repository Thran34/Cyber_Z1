using Cyber_Z1.Context;
using Cyber_Z1.Models;
using Cyber_Z1.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cyber_Z1.Controllers
{
    public class AdminController : Controller
    {
        private readonly SecurityContext _context;
        private readonly IPasswordService _passwordService;
        private ILogService _logService;
        public AdminController(SecurityContext context, IPasswordService passwordService, ILogService logService)
        {
            _context = context;
            _passwordService = passwordService;
            _logService = logService;
        }

        // GET: Admin
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
            {
                return Unauthorized();
            }

            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // GET: Admin/CreateUser
        public IActionResult CreateUser()
        {
            if (!IsAdmin())
            {
                return Unauthorized();
            }

            return View();
        }

        // POST: Admin/CreateUser
        [HttpPost]
        public async Task<IActionResult> CreateUser(User model)
        {
            if (!IsAdmin())
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                model.PasswordHash = _passwordService.HashPassword(model.PasswordHash);
                if (model.BlockedDate.HasValue && model.BlockedDate.Value < DateTime.Now)
                {
                    model.IsBlocked = true;
                }

                model.PasswordHistories = new List<PasswordHistory>();

                _context.Users.Add(model);
                await _context.SaveChangesAsync();
                await _logService.LogActivity(HttpContext.Session.GetString("Username"), "Tworzenie konta uzytkownika", $"Uzytkownik '{model.Username}' został utworzony.");
                
                return RedirectToAction("Index");
            }

            return View(model);
        }
        
        // GET: Admin/EditUser/5
        public async Task<IActionResult> EditUser(int? id)
        {
            if (!IsAdmin())
            {
                return Unauthorized();
            }

            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/EditUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, [Bind("UserId,Username,FullName,IsAdmin,IsBlocked,PasswordRestrictionsEnabled,PasswordHash")] User user)
        {
            if (!IsAdmin())
            {
                return Unauthorized();
            }

            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    var oldUsername = existingUser.Username;
                    
                    existingUser.FullName = user.FullName;
                    existingUser.IsAdmin = user.IsAdmin;
                    existingUser.IsBlocked = user.IsBlocked;
                    existingUser.PasswordRestrictionsEnabled = user.PasswordRestrictionsEnabled;

                    existingUser.PasswordHash = user.PasswordHash;

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                    await _logService.LogActivity(HttpContext.Session.GetString("Username"), "Edycja uzytkownika", $"Uzytkownik '{oldUsername}' został zaktualizowany.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }
        
        // POST: Admin/BlockUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(int id)
        {
            if (!IsAdmin())
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsBlocked = !user.IsBlocked;

            _context.Update(user);
            await _context.SaveChangesAsync();
            var action = user.IsBlocked ? "został zablokowany" : "został odblokowany";
            await _logService.LogActivity(HttpContext.Session.GetString("Username"), "Blokada uzytkownika", $"User '{user.Username}' {action}.");
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/DeleteUser/5
        public async Task<IActionResult> DeleteUser(int? id)
        {
            if (!IsAdmin())
            {
                return Unauthorized();
            }

            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Admin/DeleteUser/5
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _logService.LogActivity(HttpContext.Session.GetString("Username"), "Usuwanie uzytkownika", $"Usunięto uzytkownika {user.Username}");
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
        
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("IsAdmin") == "True";
        }

        public async Task<IActionResult> MonitorUsers()
        {
            if (!IsAdmin())
            {
                return Unauthorized();
            }

            var logs = await _context.UserActivityLogs.ToListAsync();
            return View(logs);
        }
    }
}