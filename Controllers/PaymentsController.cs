using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SHMS.Data;
using SHMS.Models;

namespace SHMS.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payments
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i!.Patient)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return View(payments);
        }

        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i!.Patient)
                .FirstOrDefaultAsync(m => m.PaymentId == id);

            if (payment == null) return NotFound();

            return View(payment);
        }

        // GET: Payments/Create
        public IActionResult Create(int? invoiceId)
        {
            ViewBag.InvoiceId = new SelectList(_context.Invoices, "InvoiceId", "InvoiceId", invoiceId);
            return View(new Payment { InvoiceId = invoiceId ?? 0 });
        }

        // POST: Payments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PaymentId,InvoiceId,PaymentDate,Amount,PaymentMethod")] Payment payment)
        {
            var invoice = await _context.Invoices.FindAsync(payment.InvoiceId);

            if (invoice == null)
            {
                ModelState.AddModelError(string.Empty, "Selected invoice not found.");
            }

            if (ModelState.IsValid)
            {
                payment.PaymentDate = DateTime.SpecifyKind(payment.PaymentDate.Date, DateTimeKind.Utc);
                _context.Add(payment);
                await _context.SaveChangesAsync();

                await UpdateInvoicePaymentStatus(payment.InvoiceId);

                return RedirectToAction("Details", "Invoices", new { id = payment.InvoiceId });
            }

            ViewBag.InvoiceId = new SelectList(_context.Invoices, "InvoiceId", "InvoiceId", payment.InvoiceId);
            return View(payment);
        }

        // GET: Payments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i!.Patient)
                .FirstOrDefaultAsync(m => m.PaymentId == id);

            if (payment == null) return NotFound();

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                var invoiceId = payment.InvoiceId;
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
                await UpdateInvoicePaymentStatus(invoiceId);
            }

            return RedirectToAction(nameof(Index));
        }

        // Recalculates and saves the invoice's PaymentStatus based on total payments received
        private async Task UpdateInvoicePaymentStatus(int invoiceId)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return;

            var totalPaid = await _context.Payments
                .Where(p => p.InvoiceId == invoiceId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            if (totalPaid <= 0)
            {
                invoice.PaymentStatus = "Unpaid";
            }
            else if (totalPaid >= invoice.TotalAmount)
            {
                invoice.PaymentStatus = "Paid";
            }
            else
            {
                invoice.PaymentStatus = "Partially Paid";
            }

            await _context.SaveChangesAsync();
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }
    }
}