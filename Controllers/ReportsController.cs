using System;
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
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reports  — menu of all available reports
        public IActionResult Index()
        {
            return View();
        }

        // Shared helper: resolves filterType (daily/weekly/monthly/custom) + optional custom dates
        // into a concrete [start, end] UTC date range.
        private (DateTime Start, DateTime End) ResolveDateRange(string? filterType, DateTime? startDate, DateTime? endDate)
        {
            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

            switch ((filterType ?? "monthly").ToLower())
            {
                case "daily":
                    return (today, today);

                case "weekly":
                    return (today.AddDays(-6), today);

                case "custom":
                    var s = startDate.HasValue
                        ? DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc)
                        : today.AddDays(-29);
                    var e = endDate.HasValue
                        ? DateTime.SpecifyKind(endDate.Value.Date, DateTimeKind.Utc)
                        : today;
                    return (s, e);

                case "monthly":
                default:
                    return (today.AddDays(-29), today);
            }
        }

        // GET: Reports/PatientReport
        public async Task<IActionResult> PatientReport(string? filterType, DateTime? startDate, DateTime? endDate)
        {
            var (start, end) = ResolveDateRange(filterType, startDate, endDate);

            var totalPatients = await _context.Patients.CountAsync();

            var patients = await _context.Patients
                .OrderBy(p => p.Name)
                .ToListAsync();

            ViewBag.FilterType = filterType ?? "monthly";
            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.TotalPatients = totalPatients;

            return View(patients);
        }

        // GET: Reports/DoctorReport
        public async Task<IActionResult> DoctorReport(string? filterType, DateTime? startDate, DateTime? endDate)
        {
            var (start, end) = ResolveDateRange(filterType, startDate, endDate);

            var doctorStats = await _context.Doctors
                .Include(d => d.Department)
                .Select(d => new DoctorReportRow
                {
                    DoctorId = d.DoctorId,
                    Name = d.Name,
                    DepartmentName = d.Department != null ? d.Department.Name : "—",
                    Specialization = d.Specialization,
                    Availability = d.Availability,
                    AppointmentCount = d.Appointments.Count(a => a.AppointmentDate >= start && a.AppointmentDate <= end)
                })
                .OrderByDescending(d => d.AppointmentCount)
                .ToListAsync();

            ViewBag.FilterType = filterType ?? "monthly";
            ViewBag.StartDate = start;
            ViewBag.EndDate = end;

            return View(doctorStats);
        }

        // GET: Reports/AppointmentReport
        public async Task<IActionResult> AppointmentReport(string? filterType, DateTime? startDate, DateTime? endDate)
        {
            var (start, end) = ResolveDateRange(filterType, startDate, endDate);

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDate >= start && a.AppointmentDate <= end)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            ViewBag.FilterType = filterType ?? "monthly";
            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.Pending = appointments.Count(a => a.Status == "Pending");
            ViewBag.Confirmed = appointments.Count(a => a.Status == "Confirmed");
            ViewBag.Completed = appointments.Count(a => a.Status == "Completed");
            ViewBag.Cancelled = appointments.Count(a => a.Status == "Cancelled");

            return View(appointments);
        }

        // GET: Reports/MedicalRecordReport
        public async Task<IActionResult> MedicalRecordReport(string? filterType, DateTime? startDate, DateTime? endDate)
        {
            var (start, end) = ResolveDateRange(filterType, startDate, endDate);

            var records = await _context.MedicalRecords
                .Include(m => m.Patient)
                .Include(m => m.Doctor)
                .Where(m => m.RecordDate >= start && m.RecordDate <= end)
                .OrderByDescending(m => m.RecordDate)
                .ToListAsync();

            ViewBag.FilterType = filterType ?? "monthly";
            ViewBag.StartDate = start;
            ViewBag.EndDate = end;

            return View(records);
        }

        // GET: Reports/PharmacyReport
        public async Task<IActionResult> PharmacyReport(string? filterType, DateTime? startDate, DateTime? endDate)
        {
            var (start, end) = ResolveDateRange(filterType, startDate, endDate);

            var purchases = await _context.Purchases
                .Include(p => p.Medicine)
                .Include(p => p.Supplier)
                .Where(p => p.PurchaseDate >= start && p.PurchaseDate <= end)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            var sales = await _context.Sales
                .Include(s => s.Medicine)
                .Include(s => s.Patient)
                .Where(s => s.SaleDate >= start && s.SaleDate <= end)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            ViewBag.FilterType = filterType ?? "monthly";
            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.TotalPurchaseAmount = purchases.Sum(p => p.TotalAmount);
            ViewBag.TotalSaleAmount = sales.Sum(s => s.TotalAmount);
            ViewBag.Sales = sales;

            return View(purchases);
        }

        // GET: Reports/LaboratoryReport
        public async Task<IActionResult> LaboratoryReport(string? filterType, DateTime? startDate, DateTime? endDate)
        {
            var (start, end) = ResolveDateRange(filterType, startDate, endDate);

            var tests = await _context.LabTests
                .Include(t => t.Patient)
                .Include(t => t.LabReport)
                .Where(t => t.TestDate >= start && t.TestDate <= end)
                .OrderByDescending(t => t.TestDate)
                .ToListAsync();

            ViewBag.FilterType = filterType ?? "monthly";
            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.TotalFees = tests.Sum(t => t.TestFee);

            return View(tests);
        }

        // GET: Reports/RevenueReport — Admin or Accountant
        [Authorize(Roles = "Super Admin,Hospital Admin,Accountant")]
        public async Task<IActionResult> RevenueReport(string? filterType, DateTime? startDate, DateTime? endDate)
        {
            var (start, end) = ResolveDateRange(filterType, startDate, endDate);

            var consultationRevenue = await _context.Invoices
                .Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end)
                .SumAsync(i => (decimal?)i.ConsultationFee) ?? 0;

            var medicineRevenue = await _context.Sales
                .Where(s => s.SaleDate >= start && s.SaleDate <= end)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            var labRevenue = await _context.LabTests
                .Where(t => t.TestDate >= start && t.TestDate <= end)
                .SumAsync(t => (decimal?)t.TestFee) ?? 0;

            var totalCollected = await _context.Payments
                .Where(p => p.PaymentDate >= start && p.PaymentDate <= end)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            ViewBag.FilterType = filterType ?? "monthly";
            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.ConsultationRevenue = consultationRevenue;
            ViewBag.MedicineRevenue = medicineRevenue;
            ViewBag.LabRevenue = labRevenue;
            ViewBag.TotalCollected = totalCollected;
            ViewBag.TotalBilled = consultationRevenue + medicineRevenue + labRevenue;

            return View();
        }

        // GET: Reports/PaymentReport — Admin or Accountant
        [Authorize(Roles = "Super Admin,Hospital Admin,Accountant")]
        public async Task<IActionResult> PaymentReport(string? filterType, DateTime? startDate, DateTime? endDate)
        {
            var (start, end) = ResolveDateRange(filterType, startDate, endDate);

            var payments = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i!.Patient)
                .Where(p => p.PaymentDate >= start && p.PaymentDate <= end)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.FilterType = filterType ?? "monthly";
            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.TotalAmount = payments.Sum(p => p.Amount);
            ViewBag.CashTotal = payments.Where(p => p.PaymentMethod == "Cash").Sum(p => p.Amount);
            ViewBag.CardTotal = payments.Where(p => p.PaymentMethod == "Card").Sum(p => p.Amount);
            ViewBag.MobileTotal = payments.Where(p => p.PaymentMethod == "Mobile Banking").Sum(p => p.Amount);

            return View(payments);
        }

        // GET: Reports/InventoryReport
        public async Task<IActionResult> InventoryReport()
        {
            var items = await _context.InventoryItems.OrderBy(i => i.ItemName).ToListAsync();
            ViewBag.LowStockCount = items.Count(i => i.Quantity < i.MinimumStock);
            return View(items);
        }

        // GET: Reports/AdmissionReport
        public async Task<IActionResult> AdmissionReport(string? filterType, DateTime? startDate, DateTime? endDate)
        {
            var (start, end) = ResolveDateRange(filterType, startDate, endDate);

            var admissions = await _context.Admissions
                .Include(a => a.Patient)
                .Include(a => a.Ward)
                .Include(a => a.Bed)
                .Where(a => a.AdmissionDate >= start && a.AdmissionDate <= end)
                .OrderByDescending(a => a.AdmissionDate)
                .ToListAsync();

            ViewBag.FilterType = filterType ?? "monthly";
            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.AdmittedCount = admissions.Count(a => a.Status == "Admitted");
            ViewBag.DischargedCount = admissions.Count(a => a.Status == "Discharged");

            return View(admissions);
        }
    }

    // Simple projection class for the Doctor Report — not a database entity.
    public class DoctorReportRow
    {
        public int DoctorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string? Specialization { get; set; }
        public bool Availability { get; set; }
        public int AppointmentCount { get; set; }
    }
}