using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SHMS.Data;
using SHMS.Models;

namespace SHMS.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public ProfileController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // GET: Profile
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound();

            return View(user);
        }

        // GET: Profile/Edit
        public async Task<IActionResult> Edit()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return NotFound();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string name, string email, string? phone)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return NotFound();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (await _context.Users.AnyAsync(u => u.Email == email && u.UserId != userId))
            {
                ModelState.AddModelError(string.Empty, "This email is already used by another account.");
                return View(user);
            }

            user.Name = name;
            user.Email = email;
            user.Phone = phone;

            await _context.SaveChangesAsync();

            TempData["ProfileUpdated"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Profile/ChangePassword
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return NotFound();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.Password, currentPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Current password is incorrect.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "New passwords do not match.");
                return View();
            }

            user.Password = _passwordHasher.HashPassword(user, newPassword);
            await _context.SaveChangesAsync();

            TempData["ProfileUpdated"] = "Your password has been changed.";
            return RedirectToAction(nameof(Index));
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var id) ? id : null;
        }
    }
}