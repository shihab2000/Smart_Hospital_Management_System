using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SHMS.Data;
using SHMS.Models;

namespace SHMS.Controllers
{
    [Authorize]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public PatientsController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // GET: Patients
        public async Task<IActionResult> Index(string searchString)
        {
            var patients = from p in _context.Patients
                            select p;

            if (!string.IsNullOrEmpty(searchString))
            {
                var search = searchString.ToLower();

                patients = patients.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    (p.Phone != null && p.Phone.ToLower().Contains(search)) ||
                    (p.BloodGroup != null && p.BloodGroup.ToLower().Contains(search)));
            }

            ViewData["CurrentFilter"] = searchString;

            return View(await patients.ToListAsync());
        }

        // GET: Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.PatientId == id);

            if (patient == null)
            {
                return NotFound();
            }

            ViewBag.MedicalRecords = await _context.MedicalRecords
                .Include(m => m.Doctor)
                .Where(m => m.PatientId == id)
                .OrderByDescending(m => m.RecordDate)
                .ToListAsync();

            ViewBag.Prescriptions = await _context.Prescriptions
                .Include(p => p.Doctor)
                .Include(p => p.PrescriptionItems)
                    .ThenInclude(pi => pi.Medicine)
                .Where(p => p.PatientId == id)
                .OrderByDescending(p => p.PrescriptionDate)
                .ToListAsync();

            return View(patient);
        }

        // GET: Patients/Create
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public async Task<IActionResult> Create(
            [Bind("PatientId,Name,DateOfBirth,Gender,Phone,Address,BloodGroup,EmergencyContact")] Patient patient,
            bool createLogin, string? loginEmail, string? loginPassword)
        {
            if (createLogin)
            {
                if (string.IsNullOrWhiteSpace(loginEmail) || string.IsNullOrWhiteSpace(loginPassword))
                {
                    ModelState.AddModelError(string.Empty, "Email and password are required to create a login account.");
                }
                else if (await _context.Users.AnyAsync(u => u.Email == loginEmail))
                {
                    ModelState.AddModelError(string.Empty, "This email is already used by another account.");
                }
            }

            if (ModelState.IsValid)
            {
                if (createLogin)
                {
                    var patientRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Patient");
                    if (patientRole == null)
                    {
                        ModelState.AddModelError(string.Empty, "Patient role not configured in the system.");
                        return View(patient);
                    }

                    var user = new User
                    {
                        Name = patient.Name,
                        Email = loginEmail!,
                        Phone = patient.Phone,
                        RoleId = patientRole.RoleId,
                        Status = "Active"
                    };
                    user.Password = _passwordHasher.HashPassword(user, loginPassword!);

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    patient.UserId = user.UserId;
                }

                _context.Add(patient);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        // GET: Patients/Edit/5
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }
            return View(patient);
        }

        // POST: Patients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public async Task<IActionResult> Edit(int id, [Bind("PatientId,Name,DateOfBirth,Gender,Phone,Address,BloodGroup,EmergencyContact,UserId")] Patient patient)
        {
            if (id != patient.PatientId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.PatientId))
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
            return View(patient);
        }

        // GET: Patients/Delete/5
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // POST: Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Hospital Admin,Receptionist")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                _context.Patients.Remove(patient);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.PatientId == id);
        }
    }
}