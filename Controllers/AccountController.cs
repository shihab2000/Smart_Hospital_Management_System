using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SHMS.Data;
using SHMS.Models;

namespace SHMS.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // GET: Account/Login
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            if (user.Status != "Active")
            {
                ModelState.AddModelError(string.Empty, "This account is inactive. Contact an administrator.");
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Patient")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            return View();
        }
       

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string name, string email, string password, string confirmPassword, string phone)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "All required fields must be filled.");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                return View();
            }

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError(string.Empty, "An account with this email already exists.");
                return View();
            }

            // Self-registration always creates a "Patient" role account
            var patientRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Patient");
            if (patientRole == null)
            {
                ModelState.AddModelError(string.Empty, "System error: Patient role not configured. Contact an administrator.");
                return View();
            }

            var user = new User
            {
                Name = name,
                Email = email,
                Phone = phone,
                RoleId = patientRole.RoleId,
                Status = "Active"
            };

            user.Password = _passwordHasher.HashPassword(user, password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["RegisterSuccess"] = "Account created successfully. Please log in.";
            return RedirectToAction(nameof(Login));
        }

        // GET: Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword — simplified: verifies email exists, then lets them set a new password directly.
        // (No email server configured, so this skips the emailed-link step.)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "No account found with this email.");
                return View();
            }

            return RedirectToAction(nameof(ResetPassword), new { email = user.Email });
        }

        // GET: Account/ResetPassword
        public IActionResult ResetPassword(string email)
        {
            ViewBag.Email = email;
            return View();
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Passwords do not match.");
                ViewBag.Email = email;
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Account not found.");
                ViewBag.Email = email;
                return View();
            }

            user.Password = _passwordHasher.HashPassword(user, newPassword);
            await _context.SaveChangesAsync();

            TempData["ResetSuccess"] = "Password reset successfully. Please log in.";
            return RedirectToAction(nameof(Login));
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}