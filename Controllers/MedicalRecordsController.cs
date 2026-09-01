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
    public class MedicalRecordsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MedicalRecordsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: MedicalRecords — Doctor sees only their own patients' records
        public async Task<IActionResult> Index()
        {
            var records = _context.MedicalRecords.Include(m => m.Doctor).Include(m => m.Patient).AsQueryable();

            if (User.IsInRole("Doctor") && !User.IsInRole("Super Admin") && !User.IsInRole("Hospital Admin"))
            {
                var myDoctorId = await GetLoggedInDoctorId();
                if (myDoctorId == null)
                {
                    return View(new List<MedicalRecord>());
                }
                records = records.Where(m => m.DoctorId == myDoctorId.Value);
            }

            return View(await records.ToListAsync());
        }

        // GET: MedicalRecords/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicalRecord = await _context.MedicalRecords
                .Include(m => m.Doctor)
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.MedicalRecordId == id);
            if (medicalRecord == null)
            {
                return NotFound();
            }

            if (!await CanAccessRecord(medicalRecord.DoctorId))
            {
                return Forbid();
            }

            return View(medicalRecord);
        }

        // GET: MedicalRecords/Create
        public IActionResult Create()
        {
            ViewData["DoctorId"] = new SelectList(_context.Doctors, "DoctorId", "Name");
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Name");
            return View();
        }

        // POST: MedicalRecords/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MedicalRecordId,PatientId,DoctorId,Symptoms,Diagnosis,Treatment,MedicalNotes,RecordDate")] MedicalRecord medicalRecord)
        {
            if (!await CanAccessRecord(medicalRecord.DoctorId))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                _context.Add(medicalRecord);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DoctorId"] = new SelectList(_context.Doctors, "DoctorId", "Name", medicalRecord.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Name", medicalRecord.PatientId);
            return View(medicalRecord);
        }

        // GET: MedicalRecords/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicalRecord = await _context.MedicalRecords.FindAsync(id);
            if (medicalRecord == null)
            {
                return NotFound();
            }

            if (!await CanAccessRecord(medicalRecord.DoctorId))
            {
                return Forbid();
            }

            ViewData["DoctorId"] = new SelectList(_context.Doctors, "DoctorId", "Name", medicalRecord.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Name", medicalRecord.PatientId);
            return View(medicalRecord);
        }

        // POST: MedicalRecords/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MedicalRecordId,PatientId,DoctorId,Symptoms,Diagnosis,Treatment,MedicalNotes,RecordDate")] MedicalRecord medicalRecord)
        {
            if (id != medicalRecord.MedicalRecordId)
            {
                return NotFound();
            }

            if (!await CanAccessRecord(medicalRecord.DoctorId))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(medicalRecord);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicalRecordExists(medicalRecord.MedicalRecordId))
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
            ViewData["DoctorId"] = new SelectList(_context.Doctors, "DoctorId", "Name", medicalRecord.DoctorId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Name", medicalRecord.PatientId);
            return View(medicalRecord);
        }

        // GET: MedicalRecords/Delete/5 — Admins only (Doctors shouldn't delete medical history)
        [Authorize(Roles = "Super Admin,Hospital Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicalRecord = await _context.MedicalRecords
                .Include(m => m.Doctor)
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.MedicalRecordId == id);
            if (medicalRecord == null)
            {
                return NotFound();
            }

            return View(medicalRecord);
        }

        // POST: MedicalRecords/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicalRecord = await _context.MedicalRecords.FindAsync(id);
            if (medicalRecord != null)
            {
                _context.MedicalRecords.Remove(medicalRecord);
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

        // Admins can access any record; a Doctor can only access records tied to their own DoctorId.
        private async Task<bool> CanAccessRecord(int recordDoctorId)
        {
            if (User.IsInRole("Super Admin") || User.IsInRole("Hospital Admin"))
            {
                return true;
            }

            if (User.IsInRole("Doctor"))
            {
                var myDoctorId = await GetLoggedInDoctorId();
                return myDoctorId.HasValue && myDoctorId.Value == recordDoctorId;
            }

            return false;
        }

        private bool MedicalRecordExists(int id)
        {
            return _context.MedicalRecords.Any(e => e.MedicalRecordId == id);
        }
    }
}