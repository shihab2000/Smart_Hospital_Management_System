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
    [Authorize(Roles = "Super Admin,Hospital Admin,Doctor")]
    public class PrescriptionItemsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PrescriptionItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
{
    var items = _context.PrescriptionItems
        .Include(p => p.Medicine)
        .Include(p => p.Prescription)
            .ThenInclude(pr => pr!.Patient)
        .AsQueryable();

    if (User.IsInRole("Doctor") && !User.IsInRole("Super Admin") && !User.IsInRole("Hospital Admin"))
    {
        var myDoctorId = await GetLoggedInDoctorId();
        if (myDoctorId == null)
        {
            return View(new List<PrescriptionItem>());
        }
        items = items.Where(i => i.Prescription != null && i.Prescription.DoctorId == myDoctorId.Value);
    }

    return View(await items.ToListAsync());
}
        // GET: PrescriptionItems/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prescriptionItem = await _context.PrescriptionItems
                .Include(p => p.Medicine)
                .Include(p => p.Prescription)
                .FirstOrDefaultAsync(m => m.PrescriptionItemId == id);
            if (prescriptionItem == null)
            {
                return NotFound();
            }

            if (!await CanAccessItem(prescriptionItem))
            {
                return Forbid();
            }

            return View(prescriptionItem);
        }

        // GET: PrescriptionItems/Create
        public IActionResult Create()
        {
            ViewData["MedicineId"] = new SelectList(_context.Medicines, "MedicineId", "MedicineName");
            ViewData["PrescriptionId"] = new SelectList(_context.Prescriptions, "PrescriptionId", "PrescriptionId");
            return View();
        }

        // POST: PrescriptionItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PrescriptionItemId,PrescriptionId,MedicineId,Dosage,Duration,Instructions")] PrescriptionItem prescriptionItem)
        {
            if (!await CanAccessItem(prescriptionItem))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                _context.Add(prescriptionItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MedicineId"] = new SelectList(_context.Medicines, "MedicineId", "MedicineName", prescriptionItem.MedicineId);
            ViewData["PrescriptionId"] = new SelectList(_context.Prescriptions, "PrescriptionId", "PrescriptionId", prescriptionItem.PrescriptionId);
            return View(prescriptionItem);
        }

        // GET: PrescriptionItems/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prescriptionItem = await _context.PrescriptionItems
                .Include(p => p.Prescription)
                .FirstOrDefaultAsync(p => p.PrescriptionItemId == id);
            if (prescriptionItem == null)
            {
                return NotFound();
            }

            if (!await CanAccessItem(prescriptionItem))
            {
                return Forbid();
            }

            ViewData["MedicineId"] = new SelectList(_context.Medicines, "MedicineId", "MedicineName", prescriptionItem.MedicineId);
            ViewData["PrescriptionId"] = new SelectList(_context.Prescriptions, "PrescriptionId", "PrescriptionId", prescriptionItem.PrescriptionId);
            return View(prescriptionItem);
        }

        // POST: PrescriptionItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PrescriptionItemId,PrescriptionId,MedicineId,Dosage,Duration,Instructions")] PrescriptionItem prescriptionItem)
        {
            if (id != prescriptionItem.PrescriptionItemId)
            {
                return NotFound();
            }

            if (!await CanAccessItem(prescriptionItem))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(prescriptionItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PrescriptionItemExists(prescriptionItem.PrescriptionItemId))
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
            ViewData["MedicineId"] = new SelectList(_context.Medicines, "MedicineId", "MedicineName", prescriptionItem.MedicineId);
            ViewData["PrescriptionId"] = new SelectList(_context.Prescriptions, "PrescriptionId", "PrescriptionId", prescriptionItem.PrescriptionId);
            return View(prescriptionItem);
        }

        // GET: PrescriptionItems/Delete/5 — Admins only
        [Authorize(Roles = "Super Admin,Hospital Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prescriptionItem = await _context.PrescriptionItems
                .Include(p => p.Medicine)
                .Include(p => p.Prescription)
                .FirstOrDefaultAsync(m => m.PrescriptionItemId == id);
            if (prescriptionItem == null)
            {
                return NotFound();
            }

            return View(prescriptionItem);
        }

        // POST: PrescriptionItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prescriptionItem = await _context.PrescriptionItems.FindAsync(id);
            if (prescriptionItem != null)
            {
                _context.PrescriptionItems.Remove(prescriptionItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

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

        // Checks access via the item's parent Prescription.DoctorId
        private async Task<bool> CanAccessItem(PrescriptionItem item)
        {
            if (User.IsInRole("Super Admin") || User.IsInRole("Hospital Admin"))
            {
                return true;
            }

            if (User.IsInRole("Doctor"))
            {
                var myDoctorId = await GetLoggedInDoctorId();
                if (!myDoctorId.HasValue) return false;

                var prescription = item.Prescription
                    ?? await _context.Prescriptions.FindAsync(item.PrescriptionId);

                return prescription != null && prescription.DoctorId == myDoctorId.Value;
            }

            return false;
        }

        private bool PrescriptionItemExists(int id)
        {
            return _context.PrescriptionItems.Any(e => e.PrescriptionItemId == id);
        }
    }
}