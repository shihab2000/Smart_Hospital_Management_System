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
    public class LabTestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LabTestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LabTests
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.LabTests.Include(l => l.Patient);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: LabTests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var labTest = await _context.LabTests
                .Include(l => l.Patient)
                .FirstOrDefaultAsync(m => m.LabTestId == id);
            if (labTest == null)
            {
                return NotFound();
            }

            return View(labTest);
        }

        // GET: LabTests/Create
        public IActionResult Create()
        {
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Gender");
            return View();
        }

        // POST: LabTests/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LabTestId,PatientId,TestName,TestDate,TestStatus,TestFee")] LabTest labTest)
        {
            if (ModelState.IsValid)
            {
                _context.Add(labTest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Gender", labTest.PatientId);
            return View(labTest);
        }

        // GET: LabTests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var labTest = await _context.LabTests.FindAsync(id);
            if (labTest == null)
            {
                return NotFound();
            }
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Gender", labTest.PatientId);
            return View(labTest);
        }

        // POST: LabTests/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LabTestId,PatientId,TestName,TestDate,TestStatus,TestFee")] LabTest labTest)
        {
            if (id != labTest.LabTestId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(labTest);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LabTestExists(labTest.LabTestId))
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
            ViewData["PatientId"] = new SelectList(_context.Patients, "PatientId", "Gender", labTest.PatientId);
            return View(labTest);
        }

        // GET: LabTests/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var labTest = await _context.LabTests
                .Include(l => l.Patient)
                .FirstOrDefaultAsync(m => m.LabTestId == id);
            if (labTest == null)
            {
                return NotFound();
            }

            return View(labTest);
        }

        // POST: LabTests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var labTest = await _context.LabTests.FindAsync(id);
            if (labTest != null)
            {
                _context.LabTests.Remove(labTest);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LabTestExists(int id)
        {
            return _context.LabTests.Any(e => e.LabTestId == id);
        }
    }
}
