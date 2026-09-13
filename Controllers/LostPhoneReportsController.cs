using Microsoft.AspNetCore.Mvc;
using MobileTracker.Data;
using MobileTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace MobileTracker.Controllers
{
    public class LostPhoneReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LostPhoneReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LostPhoneReport report)
        {
            if (!ModelState.IsValid)
            {
                return View(report);
            }

            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            report.UserId = userId.Value;
            

            // Check if Date of Loss is in the future
            if (report.DateOfLoss.Date > DateTime.UtcNow.Date)
            {
                ModelState.AddModelError(
                    "DateOfLoss",
                    "Date of loss cannot be a future date."
                );

                return View(report);
            }

            // Check if this IMEI has already been reported
            var existingReport = await _context.LostPhoneReports
                .FirstOrDefaultAsync(r => r.IMEI == report.IMEI);

            if (existingReport != null)
            {
                ModelState.AddModelError(
                    "IMEI",
                    "This IMEI has already been reported."
                );

                return View(report);
            }

            report.DateOfLoss = DateTime.SpecifyKind(
                report.DateOfLoss,
                DateTimeKind.Utc
            );

            report.SubmittedAt = DateTime.UtcNow;
            report.Status = "Submitted";

            _context.LostPhoneReports.Add(report);

            await _context.SaveChangesAsync();

            // Save initial status history
            var statusHistory = new CaseStatusHistory
            {
                CaseId = report.CaseId,
                Status = "Submitted",
                ChangedAt = DateTime.UtcNow
            };

            _context.CaseStatusHistories.Add(statusHistory);

            await _context.SaveChangesAsync();

            return RedirectToAction("Success", new { id = report.CaseId });
        }

        public IActionResult Success(int id)
        {
            ViewBag.CaseId = id;
            return View();
        }

        // =========================
        // MY REPORTS
        // =========================
        public async Task<IActionResult> MyReports()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            // User is not logged in
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get only the reports submitted by the logged-in user
            var reports = await _context.LostPhoneReports
                .Where(r => r.UserId == userId.Value)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();

            return View(reports);
        }

        // =========================
        // REPORT DETAILS
        // =========================
        
        public async Task<IActionResult> Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var report = await _context.LostPhoneReports
                .FirstOrDefaultAsync(r =>
                    r.CaseId == id &&
                    r.UserId == userId.Value);

            if (report == null)
            {
                return NotFound();
            }

            // Get GD information for this case
            var gdInfo = await _context.GDInfos
                .Include(g => g.Thana)
                .FirstOrDefaultAsync(g => g.CaseId == id);

            // Send GD information to the Details page
            ViewBag.GDInfo = gdInfo;

            return View(report);
        }
        // =========================
        // UPDATE CASE STATUS
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int caseId, string status)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Find only the logged-in user's case
            var report = await _context.LostPhoneReports
                .FirstOrDefaultAsync(r =>
                    r.CaseId == caseId &&
                    r.UserId == userId.Value);

            if (report == null)
            {
                return NotFound();
            }

            // Allowed statuses
            var allowedStatuses = new[]
            {
        "Submitted",
        "Under Review",
        "Verified",
        "Match Found",
        "Recovery in Progress",
        "Solved"
    };

            if (!allowedStatuses.Contains(status))
            {
                return BadRequest();
            }

            // Update current status
            report.Status = status;

            // If solved, save solved time
            if (status == "Solved")
            {
                report.SolvedAt = DateTime.UtcNow;
            }

            // Save status history
            var statusHistory = new CaseStatusHistory
            {
                CaseId = report.CaseId,
                Status = status,
                ChangedAt = DateTime.UtcNow
            };

            _context.CaseStatusHistories.Add(statusHistory);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                new { id = caseId }
            );
        }
    }
}