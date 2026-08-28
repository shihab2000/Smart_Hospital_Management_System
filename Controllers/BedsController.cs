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
    public class BedsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BedsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Beds
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Beds.Include(b => b.Ward);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Beds/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bed = await _context.Beds
                .Include(b => b.Ward)
                .FirstOrDefaultAsync(m => m.BedId == id);
            if (bed == null)
            {
                return NotFound();
            }

            return View(bed);
        }

        // GET: Beds/Create
        public IActionResult Create()
        {
            ViewData["WardId"] = new SelectList(_context.Wards, "WardId", "WardName");
            return View();
        }

        // POST: Beds/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BedId,WardId,BedNumber,Status")] Bed bed)
        {
            if (ModelState.IsValid)
            {
                _context.Add(bed);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["WardId"] = new SelectList(_context.Wards, "WardId", "WardName", bed.WardId);
            return View(bed);
        }

        // GET: Beds/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bed = await _context.Beds.FindAsync(id);
            if (bed == null)
            {
                return NotFound();
            }
            ViewData["WardId"] = new SelectList(_context.Wards, "WardId", "WardName", bed.WardId);
            return View(bed);
        }

        // POST: Beds/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BedId,WardId,BedNumber,Status")] Bed bed)
        {
            if (id != bed.BedId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(bed);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BedExists(bed.BedId))
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
            ViewData["WardId"] = new SelectList(_context.Wards, "WardId", "WardName", bed.WardId);
            return View(bed);
        }

        // GET: Beds/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bed = await _context.Beds
                .Include(b => b.Ward)
                .FirstOrDefaultAsync(m => m.BedId == id);
            if (bed == null)
            {
                return NotFound();
            }

            return View(bed);
        }

        // POST: Beds/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var bed = await _context.Beds.FindAsync(id);
            if (bed != null)
            {
                _context.Beds.Remove(bed);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool BedExists(int id)
        {
            return _context.Beds.Any(e => e.BedId == id);
        }
    }
}
