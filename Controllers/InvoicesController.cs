using System;
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
    [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist,Accountant")]
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Invoices
        public async Task<IActionResult> Index()
        {
            var invoices = await _context.Invoices
                .Include(i => i.Patient)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            return View(invoices);
        }

        // GET: Invoices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);

            if (invoice == null) return NotFound();

            return View(invoice);
        }

        // GET: Invoices/Create — Receptionist/Admin generate invoices (not Accountant)
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public IActionResult Create()
        {
            ViewBag.PatientId = new SelectList(_context.Patients, "PatientId", "Name");
            return View();
        }

        // GET: Invoices/GetPatientCharges?patientId=5  (AJAX helper for auto-calculation)
        [HttpGet]
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public async Task<IActionResult> GetPatientCharges(int patientId)
        {
            var medicineCharge = await _context.Sales
                .Where(s => s.PatientId == patientId)
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            var labCharge = await _context.LabTests
                .Where(t => t.PatientId == patientId)
                .SumAsync(t => (decimal?)t.TestFee) ?? 0;

            return Json(new { medicineCharge, labCharge });
        }

        // POST: Invoices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public async Task<IActionResult> Create([Bind("InvoiceId,PatientId,InvoiceDate,ConsultationFee,MedicineCharge,LabCharge")] Invoice invoice)
        {
            invoice.TotalAmount = invoice.ConsultationFee + invoice.MedicineCharge + invoice.LabCharge;
            invoice.PaymentStatus = "Unpaid";
            invoice.InvoiceDate = DateTime.SpecifyKind(invoice.InvoiceDate.Date, DateTimeKind.Utc);

            if (ModelState.IsValid)
            {
                _context.Add(invoice);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = invoice.InvoiceId });
            }

            ViewBag.PatientId = new SelectList(_context.Patients, "PatientId", "Name", invoice.PatientId);
            return View(invoice);
        }

        // GET: Invoices/Delete/5 — Admins only
        [Authorize(Roles = "Super Admin,Hospital Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);

            if (invoice == null) return NotFound();

            return View(invoice);
        }

        // POST: Invoices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Invoices/Print/5
        public async Task<IActionResult> Print(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);

            if (invoice == null) return NotFound();

            return View(invoice);
        }

        private bool InvoiceExists(int id)
        {
            return _context.Invoices.Any(e => e.InvoiceId == id);
        }
    }
}