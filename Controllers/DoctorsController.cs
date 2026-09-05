using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SHMS.Data;
using SHMS.Models;

namespace SHMS.Controllers
{
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public DoctorsController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // GET: Doctors — public, with search
        public async Task<IActionResult> Index(string searchString)
        {
            var doctors = _context.Doctors.Include(d => d.Department).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                var search = searchString.ToLower();
                doctors = doctors.Where(d =>
                    d.Name.ToLower().Contains(search) ||
                    (d.Specialization != null && d.Specialization.ToLower().Contains(search)) ||
                    (d.Department != null && d.Department.Name.ToLower().Contains(search)));
            }

            ViewData["CurrentFilter"] = searchString;

            return View(await doctors.ToListAsync());
        }

        // GET: Doctors/Details/5 — public
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .Include(d => d.DoctorSchedules)
                .FirstOrDefaultAsync(m => m.DoctorId == id);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // GET: Doctors/Create
        [Authorize(Roles = "Super Admin,Hospital Admin")]
        public IActionResult Create()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "Name");
            return View();
        }

        // POST: Doctors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin")]
        public async Task<IActionResult> Create(
            [Bind("DoctorId,Name,Email,Phone,DepartmentId,Specialization,Availability")] Doctor doctor,
            bool createLogin, string? loginPassword)
        {
            if (createLogin)
            {
                if (string.IsNullOrWhiteSpace(loginPassword))
                {
                    ModelState.AddModelError(string.Empty, "Password is required to create a login account.");
                }
                else if (await _context.Users.AnyAsync(u => u.Email == doctor.Email))
                {
                    ModelState.AddModelError(string.Empty, "This email is already used by another account.");
                }
            }

            if (ModelState.IsValid)
            {
                if (createLogin)
                {
                    var doctorRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Doctor");
                    if (doctorRole == null)
                    {
                        ModelState.AddModelError(string.Empty, "Doctor role not configured in the system.");
                        ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "Name", doctor.DepartmentId);
                        return View(doctor);
                    }

                    var user = new User
                    {
                        Name = doctor.Name,
                        Email = doctor.Email,
                        Phone = doctor.Phone,
                        RoleId = doctorRole.RoleId,
                        Status = "Active"
                    };
                    user.Password = _passwordHasher.HashPassword(user, loginPassword!);

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    doctor.UserId = user.UserId;
                }

                _context.Add(doctor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "Name", doctor.DepartmentId);
            return View(doctor);
        }

        // GET: Doctors/Edit/5
        [Authorize(Roles = "Super Admin,Hospital Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound();
            }
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "Name", doctor.DepartmentId);
            return View(doctor);
        }

        // POST: Doctors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("DoctorId,Name,Email,Phone,DepartmentId,Specialization,Availability,UserId")] Doctor doctor)
        {
            if (id != doctor.DoctorId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.DoctorId))
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
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "Name", doctor.DepartmentId);
            return View(doctor);
        }

        // GET: Doctors/Delete/5
        [Authorize(Roles = "Super Admin,Hospital Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var doctor = await _context.Doctors
                .Include(d => d.Department)
                .FirstOrDefaultAsync(m => m.DoctorId == id);
            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // POST: Doctors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Doctors/AddSchedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin,Doctor")]
        public async Task<IActionResult> AddSchedule(int doctorId, string dayOfWeek, TimeSpan startTime, TimeSpan endTime)
        {
            if (!await CanManageSchedule(doctorId))
            {
                return Forbid();
            }

            var schedule = new DoctorSchedule
            {
                DoctorId = doctorId,
                DayOfWeek = dayOfWeek,
                StartTime = startTime,
                EndTime = endTime
            };

            _context.DoctorSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = doctorId });
        }

        // POST: Doctors/DeleteSchedule
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin,Doctor")]
        public async Task<IActionResult> DeleteSchedule(int scheduleId, int doctorId)
        {
            if (!await CanManageSchedule(doctorId))
            {
                return Forbid();
            }

            var schedule = await _context.DoctorSchedules.FindAsync(scheduleId);
            if (schedule != null)
            {
                _context.DoctorSchedules.Remove(schedule);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = doctorId });
        }

        private async Task<bool> CanManageSchedule(int doctorId)
        {
            if (User.IsInRole("Super Admin") || User.IsInRole("Hospital Admin"))
            {
                return true;
            }

            if (User.IsInRole("Doctor"))
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var userId))
                {
                    var doctor = await _context.Doctors.FindAsync(doctorId);
                    return doctor != null && doctor.UserId == userId;
                }
            }

            return false;
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.DoctorId == id);
        }
    }
}