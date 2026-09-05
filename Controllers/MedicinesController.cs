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
    public class MedicinesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MedicinesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Medicines — public
        public async Task<IActionResult> Index(string filter)
        {
            var medicines = _context.Medicines.Include(m => m.Supplier).AsQueryable();

            var lowStockThreshold = 50;
            var expiryWarningDays = 30;
            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var expiryWarningDate = today.AddDays(expiryWarningDays);

            if (filter == "lowstock")
            {
                medicines = medicines.Where(m => m.Quantity < lowStockThreshold);
            }
            else if (filter == "expiring")
            {
                medicines = medicines.Where(m => m.ExpiryDate <= expiryWarningDate);
            }

            ViewData["CurrentFilter"] = filter;
            ViewData["LowStockThreshold"] = lowStockThreshold;
            ViewData["ExpiryWarningDate"] = expiryWarningDate;

            return View(await medicines.OrderBy(m => m.MedicineName).ToListAsync());
        }
        // GET: Medicines/Details/5
public async Task<IActionResult> Details(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var medicine = await _context.Medicines
        .Include(m => m.Supplier)
        .FirstOrDefaultAsync(m => m.MedicineId == id);
    if (medicine == null)
    {
        return NotFound();
    }

    return View(medicine);
}

        // GET: Medicines/Create
        [Authorize(Roles = "Super Admin,Hospital Admin,Pharmacist")]
        public IActionResult Create()
        {
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "SupplierName");
            return View();
        }

        // POST: Medicines/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin,Pharmacist")]
        public async Task<IActionResult> Create([Bind("MedicineId,MedicineName,SupplierId,Quantity,UnitPrice,ExpiryDate")] Medicine medicine)
        {
            if (ModelState.IsValid)
            {
                medicine.ExpiryDate = DateTime.SpecifyKind(medicine.ExpiryDate.Date, DateTimeKind.Utc);
                _context.Add(medicine);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "SupplierName", medicine.SupplierId);
            return View(medicine);
        }

        // GET: Medicines/Edit/5
        [Authorize(Roles = "Super Admin,Hospital Admin,Pharmacist")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null)
            {
                return NotFound();
            }
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "SupplierName", medicine.SupplierId);
            return View(medicine);
        }

        // POST: Medicines/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin,Pharmacist")]
        public async Task<IActionResult> Edit(int id, [Bind("MedicineId,MedicineName,SupplierId,Quantity,UnitPrice,ExpiryDate")] Medicine medicine)
        {
            if (id != medicine.MedicineId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    medicine.ExpiryDate = DateTime.SpecifyKind(medicine.ExpiryDate.Date, DateTimeKind.Utc);
                    _context.Update(medicine);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicineExists(medicine.MedicineId))
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
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "SupplierId", "SupplierName", medicine.SupplierId);
            return View(medicine);
        }

        // GET: Medicines/Delete/5
        [Authorize(Roles = "Super Admin,Hospital Admin,Pharmacist")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicine = await _context.Medicines
                .Include(m => m.Supplier)
                .FirstOrDefaultAsync(m => m.MedicineId == id);
            if (medicine == null)
            {
                return NotFound();
            }

            return View(medicine);
        }

        // POST: Medicines/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin,Pharmacist")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine != null)
            {
                _context.Medicines.Remove(medicine);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MedicineExists(int id)
        {
            return _context.Medicines.Any(e => e.MedicineId == id);
        }
    }
}