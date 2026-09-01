using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SHMS.Data;
using SHMS.Models;

namespace SHMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.TotalDoctors = _context.Doctors.Count();
            ViewBag.TotalDepartments = _context.Departments.Count();
            ViewBag.TotalPatients = _context.Patients.Count();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}