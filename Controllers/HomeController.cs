using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileTracker.Data;
using MobileTracker.Models;
using System.Diagnostics;

namespace MobileTracker.Controllers
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
            return View();
        }
        public async Task<IActionResult> Dashboard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            var reports = await _context.LostPhoneReports
                .Where(r => r.UserId == userId)
                .ToListAsync();

            ViewBag.UserName = user.FullName;

            ViewBag.TotalReports = reports.Count;

            ViewBag.ActiveCases = reports.Count(r =>
                r.Status == "Submitted" ||
                r.Status == "Under Investigation");

            ViewBag.ResolvedCases = reports.Count(r =>
                r.Status == "Resolved");

            return View(reports);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id
                        ?? HttpContext.TraceIdentifier
                });
        }
    }
}