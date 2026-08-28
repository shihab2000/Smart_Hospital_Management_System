using System;
using System.Collections.Generic;
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
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Admissions.Include(a => a.Bed).Include(a => a.Patient).Include(a => a.Ward);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Admissions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var admission = await _context.Admissions
                .Include(a => a.Bed)
                .Include(a => a.Patient)
                .Include(a => a.Ward)
                .FirstOrDefaultAsync(m => m.AdmissionId == id);
            if (admission == null)
            {
                return NotFound();
            }

            return View(admission);
        }

        // GET: Admissions/Create
        public IActionResult Create()
        {
            ViewData["BedId"] = new SelectList(_context.Beds, "BedId", "BedNumber");
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Gender");
            ViewData["WardId"] = new SelectList(_context.Wards, "WardId", "WardName");
            return View();
        }

        // POST: Admissions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AdmissionId,PatientId,WardId,BedId,AdmissionDate,DischargeDate,Status")] Admission admission)
        {
            if (ModelState.IsValid)
            {
                _context.Add(admission);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BedId"] = new SelectList(_context.Beds, "BedId", "BedNumber", admission.BedId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Gender", admission.PatientId);
            ViewData["WardId"] = new SelectList(_context.Wards, "WardId", "WardName", admission.WardId);
            return View(admission);
        }

        // GET: Admissions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var admission = await _context.Admissions.FindAsync(id);
            if (admission == null)
            {
                return NotFound();
            }
            ViewData["BedId"] = new SelectList(_context.Beds, "BedId", "BedNumber", admission.BedId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Gender", admission.PatientId);
            ViewData["WardId"] = new SelectList(_context.Wards, "WardId", "WardName", admission.WardId);
            return View(admission);
        }

        // POST: Admissions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AdmissionId,PatientId,WardId,BedId,AdmissionDate,DischargeDate,Status")] Admission admission)
        {
            if (id != admission.AdmissionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(admission);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdmissionExists(admission.AdmissionId))
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
            ViewData["BedId"] = new SelectList(_context.Beds, "BedId", "BedNumber", admission.BedId);
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Gender", admission.PatientId);
            ViewData["WardId"] = new SelectList(_context.Wards, "WardId", "WardName", admission.WardId);
            return View(admission);
        }

        // GET: Admissions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var admission = await _context.Admissions
                .Include(a => a.Bed)
                .Include(a => a.Patient)
                .Include(a => a.Ward)
                .FirstOrDefaultAsync(m => m.AdmissionId == id);
            if (admission == null)
            {
                return NotFound();
            }

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
