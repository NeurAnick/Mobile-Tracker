
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileTracker.Data;
using MobileTracker.Models;

namespace MobileTracker.Controllers
{
    public class FounderMessagesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FounderMessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            var messages = await _context.CaseMessages
                .Where(m => m.ReceiverId == userId.Value
                         && m.FoundReportId != null)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            var unread = messages.Where(m => !m.IsRead).ToList();
            if (unread.Any())
            {
                foreach (var m in unread)
                {
                    m.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            return View(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int messageId, string replyMessage)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(replyMessage) || replyMessage.Length > 2000)
            {
                TempData["Error"] = "Reply message is required (maximum 2000 characters).";
                return RedirectToAction(nameof(Index));
            }

            var originalMsg = await _context.CaseMessages
                .FirstOrDefaultAsync(m => m.Id == messageId && m.ReceiverId == userId.Value);

            if (originalMsg == null)
                return NotFound();

            var reply = new CaseMessage
            {
                CaseId = originalMsg.CaseId,
                FoundReportId = originalMsg.FoundReportId,
                MatchId = originalMsg.MatchId,
                SenderId = userId.Value,
                ReceiverId = originalMsg.SenderId,
                Message = replyMessage.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.CaseMessages.Add(reply);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your reply has been sent to the administrator.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessageForReport(int foundReportId, string replyMessage)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(replyMessage) || replyMessage.Length > 2000)
            {
                TempData["Error"] = "Message is required (maximum 2000 characters).";
                return RedirectToAction("Details", "FoundPhoneReports", new { id = foundReportId });
            }

            var report = await _context.FoundPhoneReports
                .FirstOrDefaultAsync(f => f.FoundReportId == foundReportId && f.UserId == userId.Value);

            if (report == null)
                return NotFound();

            var lastAdminMsg = await _context.CaseMessages
                .Where(m => m.FoundReportId == foundReportId && m.ReceiverId == userId.Value)
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefaultAsync();

            int targetAdminId = lastAdminMsg?.SenderId ?? 0;
            if (targetAdminId == 0)
            {
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
                targetAdminId = adminUser?.UserId ?? 0;
            }

            var reply = new CaseMessage
            {
                CaseId = lastAdminMsg?.CaseId ?? 0,
                FoundReportId = foundReportId,
                MatchId = lastAdminMsg?.MatchId,
                SenderId = userId.Value,
                ReceiverId = targetAdminId,
                Message = replyMessage.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.CaseMessages.Add(reply);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your reply has been sent to the administrator.";
            return RedirectToAction("Details", "FoundPhoneReports", new { id = foundReportId });
        }
    }
}
