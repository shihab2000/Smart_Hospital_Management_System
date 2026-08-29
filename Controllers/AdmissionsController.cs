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
    public class AdmissionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdmissionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admissions
        public async Task<IActionResult> Index(bool activeOnly = false)
        {
            var admissions = _context.Admissions
                .Include(a => a.Patient)
                .Include(a => a.Ward)
                .Include(a => a.Bed)
                .AsQueryable();

            if (activeOnly)
            {
                admissions = admissions.Where(a => a.Status == "Admitted");
            }

            ViewData["ActiveOnly"] = activeOnly;

            return View(await admissions
                .OrderByDescending(a => a.AdmissionDate)
                .ToListAsync());
        }

        // GET: Admissions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var admission = await _context.Admissions
                .Include(a => a.Patient)
                .Include(a => a.Ward)
                .Include(a => a.Bed)
                .FirstOrDefaultAsync(m => m.AdmissionId == id);

            if (admission == null) return NotFound();

            return View(admission);
        }

        // GET: Admissions/Create
        public IActionResult Create()
        {
            ViewBag.PatientId = new SelectList(_context.Patients, "PatientId", "Name");
            ViewBag.WardId = new SelectList(_context.Wards, "WardId", "WardName");
            ViewBag.BedId = new SelectList(
                _context.Beds.Where(b => b.Status == "Available"), "BedId", "BedNumber");
            return View();
        }

        // GET: Admissions/GetAvailableBeds?wardId=3  (AJAX helper — only show free beds in the chosen ward)
        [HttpGet]
        public async Task<IActionResult> GetAvailableBeds(int wardId)
        {
            var beds = await _context.Beds
                .Where(b => b.WardId == wardId && b.Status == "Available")
                .Select(b => new { b.BedId, b.BedNumber })
                .ToListAsync();

            return Json(beds);
        }

        // POST: Admissions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AdmissionId,PatientId,WardId,BedId,AdmissionDate,Status")] Admission admission)
        {
            var bed = await _context.Beds.FindAsync(admission.BedId);

            if (bed == null)
            {
                ModelState.AddModelError(string.Empty, "Selected bed not found.");
            }
            else if (bed.Status != "Available")
            {
                ModelState.AddModelError(string.Empty, "This bed is no longer available. Please choose another.");
            }

            admission.Status = "Admitted";
            admission.AdmissionDate = DateTime.SpecifyKind(admission.AdmissionDate.Date, DateTimeKind.Utc);
            admission.DischargeDate = null;

            if (ModelState.IsValid)
            {
                bed!.Status = "Occupied";

                _context.Add(admission);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.PatientId = new SelectList(_context.Patients, "PatientId", "Name", admission.PatientId);
            ViewBag.WardId = new SelectList(_context.Wards, "WardId", "WardName", admission.WardId);
            ViewBag.BedId = new SelectList(
                _context.Beds.Where(b => b.Status == "Available"), "BedId", "BedNumber", admission.BedId);
            return View(admission);
        }

        // POST: Admissions/Discharge/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Discharge(int id)
        {
            var admission = await _context.Admissions.FindAsync(id);
            if (admission == null) return NotFound();

            admission.Status = "Discharged";
            admission.DischargeDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

            var bed = await _context.Beds.FindAsync(admission.BedId);
            if (bed != null)
            {
                bed.Status = "Available";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = admission.AdmissionId });
        }

        // GET: Admissions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var admission = await _context.Admissions
                .Include(a => a.Patient)
                .Include(a => a.Ward)
                .Include(a => a.Bed)
                .FirstOrDefaultAsync(m => m.AdmissionId == id);

            if (admission == null) return NotFound();

            return View(admission);
        }

        // POST: Admissions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var admission = await _context.Admissions.FindAsync(id);
            if (admission != null)
            {
                // Free the bed if this admission was still active
                if (admission.Status == "Admitted")
                {
                    var bed = await _context.Beds.FindAsync(admission.BedId);
                    if (bed != null)
                    {
                        bed.Status = "Available";
                    }
                }

                _context.Admissions.Remove(admission);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AdmissionExists(int id)
        {
            return _context.Admissions.Any(e => e.AdmissionId == id);
        }
    }
}