using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SHMS.Data;
using SHMS.Models;

namespace SHMS.Controllers
{
    [Authorize(Roles = "Super Admin,Hospital Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

            var todaysAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDate.Date == today)
                .OrderBy(a => a.AppointmentTime)
                .ToListAsync();

            ViewBag.TodaysAppointmentCount = todaysAppointments.Count;
            ViewBag.TotalPatients = await _context.Patients.CountAsync();
            ViewBag.TotalDoctors = await _context.Doctors.CountAsync();

            ViewBag.TodaysRevenue = await _context.Payments
                .Where(p => p.PaymentDate.Date == today)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            ViewBag.TotalAdmitted = await _context.Admissions
                .CountAsync(a => a.Status == "Admitted");

            ViewBag.AvailableBeds = await _context.Beds
                .CountAsync(b => b.Status == "Available");
            ViewBag.TotalBeds = await _context.Beds.CountAsync();

            ViewBag.LowStockMedicineCount = await _context.Medicines
                .CountAsync(m => m.Quantity < 50);
            ViewBag.TotalMedicines = await _context.Medicines.CountAsync();

            ViewBag.RecentAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentPayments = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i!.Patient)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToListAsync();

            return View(todaysAppointments);
        }
    }
}