using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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

        public DoctorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Doctors — public
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Doctors.Include(d => d.Department);
            return View(await applicationDbContext.ToListAsync());
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
        public async Task<IActionResult> Create([Bind("DoctorId,Name,Email,Phone,DepartmentId,Specialization,Availability")] Doctor doctor)
        {
            if (ModelState.IsValid)
            {
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
        public async Task<IActionResult> Edit(int id, [Bind("DoctorId,Name,Email,Phone,DepartmentId,Specialization,Availability")] Doctor doctor)
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

        // A Doctor-role user may only manage the schedule of the Doctor record linked to their own account.
        // Admins can manage any doctor's schedule.
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