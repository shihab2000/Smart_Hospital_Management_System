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
    public class PrescriptionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PrescriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Prescriptions — Doctor sees only their own
        public async Task<IActionResult> Index()
        {
            var prescriptions = _context.Prescriptions.Include(p => p.Doctor).Include(p => p.Patient).AsQueryable();

            if (User.IsInRole("Doctor") && !User.IsInRole("Super Admin") && !User.IsInRole("Hospital Admin"))
            {
                var myDoctorId = await GetLoggedInDoctorId();
                if (myDoctorId == null)
                {
                    return View(new List<Prescription>());
                }
                prescriptions = prescriptions.Where(p => p.DoctorId == myDoctorId.Value);
            }

            return View(await prescriptions.ToListAsync());
        }

        // GET: Prescriptions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prescription = await _context.Prescriptions
                .Include(p => p.Doctor)
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(m => m.PrescriptionId == id);
            if (prescription == null)
            {
                return NotFound();
            }

            if (!await CanAccessPrescription(prescription.DoctorId))
            {
                return Forbid();
            }

            return View(prescription);
        }

        // GET: Prescriptions/Create
        public IActionResult Create()
        {
            ViewData["DoctorId"] = new SelectList(_context.Doctors, "DoctorId", "Name");
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Name");
            return View();
        }

        // POST: Prescriptions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PrescriptionId,PatientId,DoctorId,PrescriptionDate,Notes")] Prescription prescription)
        {
            if (!await CanAccessPrescription(prescription.DoctorId))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                _context.Add(prescription);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DoctorId"] = new SelectList(_context.Doctors, "DoctorId", "Name", prescription.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Name", prescription.PatientId);
            return View(prescription);
        }

        // GET: Prescriptions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null)
            {
                return NotFound();
            }

            if (!await CanAccessPrescription(prescription.DoctorId))
            {
                return Forbid();
            }

            ViewData["DoctorId"] = new SelectList(_context.Doctors, "DoctorId", "Name", prescription.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Name", prescription.PatientId);
            return View(prescription);
        }

        // POST: Prescriptions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PrescriptionId,PatientId,DoctorId,PrescriptionDate,Notes")] Prescription prescription)
        {
            if (id != prescription.PrescriptionId)
            {
                return NotFound();
            }

            if (!await CanAccessPrescription(prescription.DoctorId))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(prescription);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PrescriptionExists(prescription.PrescriptionId))
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
            ViewData["DoctorId"] = new SelectList(_context.Doctors, "DoctorId", "Name", prescription.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Name", prescription.PatientId);
            return View(prescription);
        }

        // GET: Prescriptions/Delete/5 — Admins only
        [Authorize(Roles = "Super Admin,Hospital Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prescription = await _context.Prescriptions
                .Include(p => p.Doctor)
                .Include(p => p.Patient)
                .FirstOrDefaultAsync(m => m.PrescriptionId == id);
            if (prescription == null)
            {
                return NotFound();
            }

            return View(prescription);
        }

        // POST: Prescriptions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription != null)
            {
                _context.Prescriptions.Remove(prescription);
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

        private async Task<bool> CanAccessPrescription(int prescriptionDoctorId)
        {
            if (User.IsInRole("Super Admin") || User.IsInRole("Hospital Admin"))
            {
                return true;
            }

            if (User.IsInRole("Doctor"))
            {
                var myDoctorId = await GetLoggedInDoctorId();
                return myDoctorId.HasValue && myDoctorId.Value == prescriptionDoctorId;
            }

            return false;
        }

        private bool PrescriptionExists(int id)
        {
            return _context.Prescriptions.Any(e => e.PrescriptionId == id);
        }
    }
}