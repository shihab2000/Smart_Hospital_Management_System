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
    [Authorize]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Appointments  (?today=true for today's appointments only)
        public async Task<IActionResult> Index(bool today = false)
        {
            var appointments = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsQueryable();

            // Doctor role: only see their own appointments
            if (User.IsInRole("Doctor") && !User.IsInRole("Super Admin") && !User.IsInRole("Hospital Admin"))
            {
                var myDoctorId = await GetLoggedInDoctorId();
                if (myDoctorId == null)
                {
                    return View(new List<Appointment>());
                }
                appointments = appointments.Where(a => a.DoctorId == myDoctorId.Value);
            }

            if (today)
            {
                var todayDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
                appointments = appointments.Where(a => a.AppointmentDate.Date == todayDate);
            }

            ViewData["TodayOnly"] = today;

            return View(await appointments
                .OrderByDescending(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToListAsync());
        }

        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();

            if (!await CanAccessAppointment(appointment))
            {
                return Forbid();
            }

            return View(appointment);
        }

        // GET: Appointments/Create
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public IActionResult Create()
        {
            ViewBag.PatientId = new SelectList(_context.Patients, "PatientId", "Name");
            ViewBag.DoctorId = new SelectList(_context.Doctors, "DoctorId", "Name");
            return View();
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public async Task<IActionResult> Create([Bind("AppointmentId,PatientId,DoctorId,AppointmentDate,AppointmentTime,Reason,Status")] Appointment appointment)
        {
            appointment.AppointmentDate = DateTime.SpecifyKind(appointment.AppointmentDate.Date, DateTimeKind.Utc);

            var availability = await CheckDoctorAvailability(appointment.DoctorId, appointment.AppointmentDate, appointment.AppointmentTime, null);
            if (!availability.IsAvailable)
            {
                ModelState.AddModelError(string.Empty, availability.Message);
            }

            if (ModelState.IsValid)
            {
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.PatientId = new SelectList(_context.Patients, "PatientId", "Name", appointment.PatientId);
            ViewBag.DoctorId = new SelectList(_context.Doctors, "DoctorId", "Name", appointment.DoctorId);
            return View(appointment);
        }

        // GET: Appointments/Edit/5 — Admin/Receptionist for full edit, or the assigned Doctor (status only, enforced in view/POST)
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist,Doctor")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            if (!await CanAccessAppointment(appointment))
            {
                return Forbid();
            }

            ViewBag.PatientId = new SelectList(_context.Patients, "PatientId", "Name", appointment.PatientId);
            ViewBag.DoctorId = new SelectList(_context.Doctors, "DoctorId", "Name", appointment.DoctorId);
            return View(appointment);
        }

        // POST: Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist,Doctor")]
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,PatientId,DoctorId,AppointmentDate,AppointmentTime,Reason,Status")] Appointment appointment)
        {
            if (id != appointment.AppointmentId) return NotFound();

            if (!await CanAccessAppointment(appointment))
            {
                return Forbid();
            }

            appointment.AppointmentDate = DateTime.SpecifyKind(appointment.AppointmentDate.Date, DateTimeKind.Utc);

            var availability = await CheckDoctorAvailability(appointment.DoctorId, appointment.AppointmentDate, appointment.AppointmentTime, appointment.AppointmentId);
            if (!availability.IsAvailable)
            {
                ModelState.AddModelError(string.Empty, availability.Message);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.AppointmentId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.PatientId = new SelectList(_context.Patients, "PatientId", "Name", appointment.PatientId);
            ViewBag.DoctorId = new SelectList(_context.Doctors, "DoctorId", "Name", appointment.DoctorId);
            return View(appointment);
        }

        // GET: Appointments/Delete/5
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Appointments/Cancel/5 — Admin/Receptionist any appointment, Doctor only their own
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist,Doctor")]
        public async Task<IActionResult> Cancel(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            if (!await CanAccessAppointment(appointment))
            {
                return Forbid();
            }

            appointment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Reschedule/5
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public async Task<IActionResult> Reschedule(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // POST: Appointments/Reschedule/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public async Task<IActionResult> Reschedule(int id, DateTime appointmentDate, TimeSpan appointmentTime)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            var normalizedDate = DateTime.SpecifyKind(appointmentDate.Date, DateTimeKind.Utc);

            var availability = await CheckDoctorAvailability(appointment.DoctorId, normalizedDate, appointmentTime, appointment.AppointmentId);
            if (!availability.IsAvailable)
            {
                ModelState.AddModelError(string.Empty, availability.Message);
                var reloaded = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.AppointmentId == id);
                return View(reloaded);
            }

            appointment.AppointmentDate = normalizedDate;
            appointment.AppointmentTime = appointmentTime;
            appointment.Status = "Pending";

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = appointment.AppointmentId });
        }

        // Shared availability check: doctor's weekly schedule + no double-booking
        private async Task<(bool IsAvailable, string Message)> CheckDoctorAvailability(int doctorId, DateTime date, TimeSpan time, int? excludingAppointmentId)
        {
            var normalizedDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            var dayName = normalizedDate.DayOfWeek.ToString();

            var hasSchedule = await _context.DoctorSchedules
                .AnyAsync(s => s.DoctorId == doctorId
                    && s.DayOfWeek == dayName
                    && time >= s.StartTime
                    && time <= s.EndTime);

            if (!hasSchedule)
            {
                return (false, $"This doctor is not scheduled to work on {dayName} at that time.");
            }

            var conflictQuery = _context.Appointments.Where(a =>
                a.DoctorId == doctorId &&
                a.AppointmentDate.Date == normalizedDate &&
                a.AppointmentTime == time &&
                a.Status != "Cancelled");

            if (excludingAppointmentId.HasValue)
            {
                conflictQuery = conflictQuery.Where(a => a.AppointmentId != excludingAppointmentId.Value);
            }

            var hasConflict = await conflictQuery.AnyAsync();
            if (hasConflict)
            {
                return (false, "This doctor already has an appointment at that exact date and time.");
            }

            return (true, string.Empty);
        }

        // Resolves the DoctorId linked to the currently logged-in User (via Doctor.UserId), if any.
        private async Task<int?> GetLoggedInDoctorId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            return doctor?.DoctorId;
        }

        // Admins/Receptionist can access any appointment.
        // A Doctor-role user can only access appointments assigned to their own linked Doctor record.
        private async Task<bool> CanAccessAppointment(Appointment appointment)
        {
            if (User.IsInRole("Super Admin") || User.IsInRole("Hospital Admin") || User.IsInRole("Receptionist"))
            {
                return true;
            }

            if (User.IsInRole("Doctor"))
            {
                var myDoctorId = await GetLoggedInDoctorId();
                return myDoctorId.HasValue && myDoctorId.Value == appointment.DoctorId;
            }

            return false;
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.AppointmentId == id);
        }
    }
}