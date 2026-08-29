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
    public class LabReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LabReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: LabReports
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.LabReports.Include(l => l.LabTest);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: LabReports/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var labReport = await _context.LabReports
                .Include(l => l.LabTest)
                .FirstOrDefaultAsync(m => m.LabReportId == id);
            if (labReport == null)
            {
                return NotFound();
            }

            return View(labReport);
        }

        // GET: LabReports/Create
        public IActionResult Create()
        {
            ViewData["LabTestId"] = new SelectList(_context.LabTests, "LabTestId", "TestName");
            return View();
        }

        // POST: LabReports/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("LabReportId,LabTestId,Result,Remarks,ReportDate")] LabReport labReport)
        {
            if (ModelState.IsValid)
            {
                _context.Add(labReport);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["LabTestId"] = new SelectList(_context.LabTests, "LabTestId", "TestName", labReport.LabTestId);
            return View(labReport);
        }

        // GET: LabReports/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var labReport = await _context.LabReports.FindAsync(id);
            if (labReport == null)
            {
                return NotFound();
            }
            ViewData["LabTestId"] = new SelectList(_context.LabTests, "LabTestId", "TestName", labReport.LabTestId);
            return View(labReport);
        }

        // POST: LabReports/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LabReportId,LabTestId,Result,Remarks,ReportDate")] LabReport labReport)
        {
            if (id != labReport.LabReportId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(labReport);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LabReportExists(labReport.LabReportId))
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
            ViewData["LabTestId"] = new SelectList(_context.LabTests, "LabTestId", "TestName", labReport.LabTestId);
            return View(labReport);
        }

        // GET: LabReports/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var labReport = await _context.LabReports
                .Include(l => l.LabTest)
                .FirstOrDefaultAsync(m => m.LabReportId == id);
            if (labReport == null)
            {
                return NotFound();
            }

            return View(labReport);
        }

        // POST: LabReports/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var labReport = await _context.LabReports.FindAsync(id);
            if (labReport != null)
            {
                _context.LabReports.Remove(labReport);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LabReportExists(int id)
        {
            return _context.LabReports.Any(e => e.LabReportId == id);
        }


        // GET: LabReports/Print/5
        public async Task<IActionResult> Print(int? id)
        {
            if (id == null) return NotFound();

            var report = await _context.LabReports
                .Include(r => r.LabTest)
                    .ThenInclude(t => t!.Patient)
                .FirstOrDefaultAsync(r => r.LabReportId == id);

            if (report == null) return NotFound();

            return View(report);
        }
    }
}
