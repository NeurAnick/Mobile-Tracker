using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileTracker.Data;
using MobileTracker.Models;

namespace MobileTracker.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Setup()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(
            string email,
            string setupCode)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(setupCode))
            {
                ViewBag.Error = "Email and setup code are required.";
                return View();
            }

            var configuredCode =
                _configuration["AdminSetup:SetupCode"];

            if (setupCode != configuredCode)
            {
                ViewBag.Error = "Invalid setup code.";
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ViewBag.Error = "No user found with this email.";
                return View();
            }

            user.Role = "Admin";

            await _context.SaveChangesAsync();

            ViewBag.Success =
                "Admin role has been assigned successfully.";

            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return Forbid();
            }

            var totalReports = await _context.LostPhoneReports.CountAsync();

            var totalCitizens = await _context.Users
                .CountAsync(u => u.Role == "Citizen");

            var totalThanas = await _context.Thanas.CountAsync();

            var totalReviews = await _context.ThanaReviews.CountAsync();

            var solvedCases = await _context.CaseOutcomes
                .CountAsync(o => o.Outcome == "Solved");

            var unresolvedCases = await _context.CaseOutcomes
                .CountAsync(o => o.Outcome == "Unresolved");

            var pendingCases = await _context.CaseOutcomes
                .CountAsync(o => o.Outcome == "Unknown");

            ViewBag.AdminName =
                HttpContext.Session.GetString("UserName") ?? "Admin";

            ViewBag.TotalReports = totalReports;
            ViewBag.TotalCitizens = totalCitizens;
            ViewBag.TotalThanas = totalThanas;
            ViewBag.TotalReviews = totalReviews;

            ViewBag.SolvedCases = solvedCases;
            ViewBag.UnresolvedCases = unresolvedCases;
            ViewBag.PendingCases = pendingCases;

            var adminUserId = HttpContext.Session.GetInt32("UserId");
            if (adminUserId != null)
            {
                ViewBag.UnreadMessages = await _context.CaseMessages
                    .CountAsync(m => m.ReceiverId == adminUserId.Value && !m.IsRead);
            }

            return View();
        }

        public async Task<IActionResult> Citizens()
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return Forbid();
            }

            var citizens = await _context.Users
                .Where(u => u.Role == "Citizen")
                .OrderBy(u => u.UserId).ToListAsync();

            return View(citizens);
        }

        public async Task<IActionResult> CitizenDetails(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return Forbid();
            }

            var citizen = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id && u.Role == "Citizen");

            if (citizen == null)
            {
                return NotFound();
            }

            var pendingApplications = await _context.PendingCases
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.SubmittedAt)
                .ToListAsync();

            var approvedCases = await _context.LostPhoneReports
                .Where(r => r.UserId == id)
                .OrderByDescending(r => r.SubmittedAt)
                .ToListAsync();

            ViewBag.PendingApplications = pendingApplications;
            ViewBag.ApprovedCases = approvedCases;

            return View(citizen);
        }

        public async Task<IActionResult> Statistics()
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return Forbid();
            }

            ViewBag.TotalApplications =
                await _context.PendingCases.CountAsync();

            ViewBag.PendingApplications =
                await _context.PendingCases
                    .CountAsync(p => p.VerificationStatus == "Pending");

            ViewBag.ApprovedApplications =
                await _context.PendingCases
                    .CountAsync(p => p.VerificationStatus == "Approved");

            ViewBag.RejectedApplications =
                await _context.PendingCases
                    .CountAsync(p => p.VerificationStatus == "Rejected");

            ViewBag.AdditionalInformation =
                await _context.PendingCases
                    .CountAsync(p =>
                        p.VerificationStatus == "Additional Information Required");

            ViewBag.TotalCases =
                await _context.LostPhoneReports.CountAsync();

            ViewBag.VerifiedCases =
                await _context.LostPhoneReports
                    .CountAsync(r => r.VerificationStatus == "Approved");

            ViewBag.SolvedCases =
                await _context.CaseOutcomes
                    .CountAsync(o => o.Outcome == "Solved");

            ViewBag.UnresolvedCases =
                await _context.CaseOutcomes
                    .CountAsync(o => o.Outcome == "Unresolved");

            ViewBag.UnknownCases =
                await _context.CaseOutcomes
                    .CountAsync(o => o.Outcome == "Unknown");

            return View();
        }

        public async Task<IActionResult> Cases()
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return Forbid();
            }

            var pendingCases = await _context.PendingCases
                .Include(p => p.User)
                .Include(p => p.Thana)
                .OrderByDescending(p => p.SubmittedAt)
                .ToListAsync();

            return View(pendingCases);
        }

        public async Task<IActionResult> PendingCaseDetails(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return Forbid();
            }

            var pendingCase = await _context.PendingCases
                .Include(p => p.User)
                .Include(p => p.Thana)
                .FirstOrDefaultAsync(p => p.PendingCaseId == id);

            if (pendingCase == null)
            {
                return NotFound();
            }

            return View(pendingCase);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewPendingCase(
    int id,
    string decision,
    string? adminNote)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return Forbid();
            }

            var adminId = HttpContext.Session.GetInt32("UserId");

            if (adminId == null)
            {
                return Forbid();
            }

            var pendingCase = await _context.PendingCases
                .FirstOrDefaultAsync(p => p.PendingCaseId == id);

            if (pendingCase == null)
            {
                return NotFound();
            }

            // Normalize decision
            decision = decision?.Trim() ?? "";

            if (decision != "Approved" &&
                decision != "Rejected" &&
                decision != "Additional Information Required")
            {
                TempData["Error"] = "Invalid review decision.";
                return RedirectToAction(
                    nameof(PendingCaseDetails),
                    new { id });
            }

            // Already approved
            if (pendingCase.VerificationStatus == "Approved")
            {
                TempData["Error"] =
                    "This application has already been approved.";

                return RedirectToAction(
                    nameof(PendingCaseDetails),
                    new { id });
            }


            // ============================================================
            // REJECT
            // ============================================================

            if (decision == "Rejected")
            {
                pendingCase.VerificationStatus = "Rejected";
                pendingCase.ReviewedAt = DateTime.UtcNow;
                pendingCase.ReviewedByAdminId = adminId.Value;
                pendingCase.AdminNote =
                    string.IsNullOrWhiteSpace(adminNote)
                        ? "Application rejected by administrator."
                        : adminNote.Trim();

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Application has been rejected successfully.";

                return RedirectToAction(
                    nameof(PendingCaseDetails),
                    new { id });
            }


            // ============================================================
            // ADDITIONAL INFORMATION REQUIRED
            // ============================================================

            if (decision == "Additional Information Required")
            {
                pendingCase.VerificationStatus =
                    "Additional Information Required";

                pendingCase.ReviewedAt = DateTime.UtcNow;
                pendingCase.ReviewedByAdminId = adminId.Value;

                pendingCase.AdminNote =
                    string.IsNullOrWhiteSpace(adminNote)
                        ? "Additional information is required."
                        : adminNote.Trim();

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Additional information has been requested.";

                return RedirectToAction(
                    nameof(PendingCaseDetails),
                    new { id });
            }


            // ============================================================
            // APPROVED
            // ============================================================

            if (decision == "Approved")
            {
                // --------------------------------------------
                // Duplicate IMEI
                // --------------------------------------------

                var existingCase = await _context.LostPhoneReports
                    .FirstOrDefaultAsync(r =>
                        r.IMEI == pendingCase.IMEI);

                if (existingCase != null)
                {
                    pendingCase.VerificationStatus = "Rejected";
                    pendingCase.ReviewedAt = DateTime.UtcNow;
                    pendingCase.ReviewedByAdminId = adminId.Value;
                    pendingCase.AdminNote =
                        "Application rejected because this IMEI already exists.";

                    await _context.SaveChangesAsync();

                    TempData["Error"] =
                        "This IMEI already exists in an official case.";

                    return RedirectToAction(
                        nameof(PendingCaseDetails),
                        new { id });
                }


                // --------------------------------------------
                // Duplicate GD Number
                // --------------------------------------------

                var existingGD = await _context.GDInfos
                    .FirstOrDefaultAsync(g =>
                        g.GDNumber == pendingCase.GDNumber);

                if (existingGD != null)
                {
                    pendingCase.VerificationStatus = "Rejected";
                    pendingCase.ReviewedAt = DateTime.UtcNow;
                    pendingCase.ReviewedByAdminId = adminId.Value;
                    pendingCase.AdminNote =
                        "Application rejected because this GD number already exists.";

                    await _context.SaveChangesAsync();

                    TempData["Error"] =
                        "This GD number already exists.";

                    return RedirectToAction(
                        nameof(PendingCaseDetails),
                        new { id });
                }


                // --------------------------------------------
                // TRANSACTION
                // --------------------------------------------

                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    // ----------------------------------------
                    // Create Official Case
                    // ----------------------------------------

                    var report = new LostPhoneReport
                    {
                        UserId = pendingCase.UserId,

                        Brand = pendingCase.Brand,
                        Model = pendingCase.Model,
                        Color = pendingCase.Color,
                        IMEI = pendingCase.IMEI,

                        DateOfLoss = pendingCase.DateOfLoss,
                        LocationLost = pendingCase.LocationLost,
                        RewardAmount = pendingCase.RewardAmount,

                        Status = "Verified",

                        SubmittedAt = pendingCase.SubmittedAt,

                        VerificationStatus = "Approved",
                        VerifiedAt = DateTime.UtcNow,
                        VerifiedByAdminId = adminId.Value,
                        AdminNote = adminNote?.Trim()
                    };

                    _context.LostPhoneReports.Add(report);

                    await _context.SaveChangesAsync();


                    // ----------------------------------------
                    // GD Information
                    // ----------------------------------------

                    var gdInfo = new GDInfo
                    {
                        GDNumber = pendingCase.GDNumber,
                        ThanaId = pendingCase.ThanaId,
                        GDPhotoPath = pendingCase.GDPhotoPath,
                        CaseId = report.CaseId
                    };

                    _context.GDInfos.Add(gdInfo);


                    // ----------------------------------------
                    // Case Outcome
                    // ----------------------------------------

                    var outcome = new CaseOutcome
                    {
                        CaseId = report.CaseId,
                        Outcome = "Unknown",
                        RecordedAt = DateTime.UtcNow
                    };

                    _context.CaseOutcomes.Add(outcome);


                    // ----------------------------------------
                    // Case History
                    // ----------------------------------------

                    var history = new CaseStatusHistory
                    {
                        CaseId = report.CaseId,
                        Status = "Verified",
                        ChangedAt = DateTime.UtcNow
                    };

                    _context.CaseStatusHistories.Add(history);


                    // ----------------------------------------
                    // Update Pending Application
                    // ----------------------------------------

                    pendingCase.VerificationStatus = "Approved";
                    pendingCase.ReviewedAt = DateTime.UtcNow;
                    pendingCase.ReviewedByAdminId = adminId.Value;
                    pendingCase.AdminNote = adminNote?.Trim();


                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();


                    TempData["Success"] =
                        $"Application approved. Official Case #{report.CaseId} created.";

                    return RedirectToAction(
                        nameof(CaseDetails),
                        new { id = report.CaseId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    TempData["Error"] =
                        "Could not approve the application: " + ex.Message;

                    return RedirectToAction(
                        nameof(PendingCaseDetails),
                        new { id });
                }
            }


            return RedirectToAction(
                nameof(PendingCaseDetails),
                new { id });
        }


        public async Task<IActionResult> CaseDetails(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return Forbid();
            }

            var report = await _context.LostPhoneReports
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.CaseId == id);

            if (report == null)
            {
                return NotFound();
            }

            var gdInfo = await _context.GDInfos
                .Include(g => g.Thana)
                .FirstOrDefaultAsync(g => g.CaseId == id);

            var outcome = await _context.CaseOutcomes
                .FirstOrDefaultAsync(o => o.CaseId == id);

            var statusHistory = await _context.CaseStatusHistories
                .Where(h => h.CaseId == id)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();

            var messages = await _context.CaseMessages
                .Where(m => m.CaseId == id && m.FoundReportId == null)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var currentAdminId = HttpContext.Session.GetInt32("UserId");
            if (currentAdminId != null)
            {
                var unreadReplies = messages
                    .Where(m => m.ReceiverId == currentAdminId.Value && !m.IsRead)
                    .ToList();
                if (unreadReplies.Any())
                {
                    foreach (var m in unreadReplies)
                    {
                        m.IsRead = true;
                    }
                    await _context.SaveChangesAsync();
                }
            }

            ViewBag.GDInfo = gdInfo;
            ViewBag.CaseOutcome = outcome;
            ViewBag.StatusHistory = statusHistory;
            ViewBag.Messages = messages;

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCase(
            int id,
            string decision,
            string? adminNote)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return Forbid();
            }

            var report = await _context.LostPhoneReports
                .FirstOrDefaultAsync(r => r.CaseId == id);

            if (report == null)
            {
                return NotFound();
            }

            var adminId = HttpContext.Session.GetInt32("UserId");
            if (adminId == null)
            {
                return Forbid();
            }

            decision = decision?.Trim() ?? "";

            if (decision != "Approved" &&
                decision != "Rejected" &&
                decision != "Additional Information Required")
            {
                return BadRequest("Invalid verification decision.");
            }

            report.VerificationStatus = decision;
            report.VerifiedAt = DateTime.UtcNow;
            report.VerifiedByAdminId = adminId.Value;
            report.AdminNote = adminNote?.Trim();

            if (decision == "Approved")
            {
                report.Status = "Verified";

                var history = new CaseStatusHistory
                {
                    CaseId = report.CaseId,
                    Status = "Verified",
                    ChangedAt = DateTime.UtcNow
                };

                _context.CaseStatusHistories.Add(history);

                _context.CaseMessages.Add(new CaseMessage
                {
                    CaseId = report.CaseId,
                    SenderId = adminId.Value,
                    ReceiverId = report.UserId,
                    Message = string.IsNullOrWhiteSpace(adminNote)
                        ? $"Your lost phone report for Case #{report.CaseId} has been approved and verified by the administrator."
                        : $"Your lost phone report for Case #{report.CaseId} has been approved and verified. Note: {adminNote.Trim()}",
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                });
            }
            else if (decision == "Rejected")
            {
                report.Status = "Rejected";

                var history = new CaseStatusHistory
                {
                    CaseId = report.CaseId,
                    Status = "Rejected",
                    ChangedAt = DateTime.UtcNow
                };

                _context.CaseStatusHistories.Add(history);

                _context.CaseMessages.Add(new CaseMessage
                {
                    CaseId = report.CaseId,
                    SenderId = adminId.Value,
                    ReceiverId = report.UserId,
                    Message = string.IsNullOrWhiteSpace(adminNote)
                        ? $"Your lost phone report for Case #{report.CaseId} has been rejected by the administrator."
                        : $"Your lost phone report for Case #{report.CaseId} has been rejected. Reason: {adminNote.Trim()}",
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                });
            }
            else if (decision == "Additional Information Required")
            {
                _context.CaseMessages.Add(new CaseMessage
                {
                    CaseId = report.CaseId,
                    SenderId = adminId.Value,
                    ReceiverId = report.UserId,
                    Message = string.IsNullOrWhiteSpace(adminNote)
                        ? $"Additional information is required for your Case #{report.CaseId}."
                        : $"Additional information is required for your Case #{report.CaseId}: {adminNote.Trim()}",
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = decision switch
            {
                "Approved" => "Case approved and verified successfully.",
                "Rejected" => "Case rejected and owner notified.",
                _ => "Additional information requested from owner."
            };

            return RedirectToAction(
                nameof(CaseDetails),
                new { id = report.CaseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestOwnerInformation(
            int caseId,
            string message)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return Forbid();

            var adminId = HttpContext.Session.GetInt32("UserId");
            if (adminId == null)
                return Forbid();

            if (string.IsNullOrWhiteSpace(message) || message.Length > 2000)
            {
                TempData["Error"] = "Message is required (maximum 2000 characters).";
                return RedirectToAction(nameof(CaseDetails), new { id = caseId });
            }

            var report = await _context.LostPhoneReports
                .FirstOrDefaultAsync(r => r.CaseId == caseId);

            if (report == null)
                return NotFound();

            _context.CaseMessages.Add(new CaseMessage
            {
                CaseId = report.CaseId,
                SenderId = adminId.Value,
                ReceiverId = report.UserId,
                Message = message.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Request for additional information sent to the owner.";
            return RedirectToAction(nameof(CaseDetails), new { id = caseId });
        }

        public async Task<IActionResult> FoundReports()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return Forbid();

            var reports = await _context.FoundPhoneReports
                .Include(f => f.User)
                .OrderByDescending(f => f.SubmittedAt)
                .ToListAsync();

            return View(reports);
        }

        public async Task<IActionResult> Matches()
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return Forbid();
            }

            var matches = await _context.CaseMatches
                .Include(m => m.LostPhoneReport)
                .Include(m => m.FoundPhoneReport)
                    .ThenInclude(f => f!.User)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(matches);
        }

        public async Task<IActionResult> MatchDetails(int id)
        {
            var role = HttpContext.Session.GetString("Role");

            if (role != "Admin")
            {
                return Forbid();
            }

            var match = await _context.CaseMatches
                .Include(m => m.LostPhoneReport)
                    .ThenInclude(l => l!.User)
                .Include(m => m.FoundPhoneReport)
                    .ThenInclude(f => f!.User)
                .FirstOrDefaultAsync(m => m.MatchId == id);

            if (match == null)
            {
                return NotFound();
            }

            var gdInfo = await _context.GDInfos
                .Include(g => g.Thana)
                .FirstOrDefaultAsync(g =>
                    g.CaseId == match.CaseId);

            var messages = await _context.CaseMessages
                .Where(m => m.MatchId == id || (m.CaseId == match.CaseId && (m.FoundReportId == null || m.FoundReportId == match.FoundReportId)))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var currentAdminId = HttpContext.Session.GetInt32("UserId");
            if (currentAdminId != null)
            {
                var unreadReplies = messages
                    .Where(m => m.ReceiverId == currentAdminId.Value && !m.IsRead)
                    .ToList();
                if (unreadReplies.Any())
                {
                    foreach (var m in unreadReplies)
                    {
                        m.IsRead = true;
                    }
                    await _context.SaveChangesAsync();
                }
            }

            ViewBag.GDInfo = gdInfo;
            ViewBag.Messages = messages;

            return View(match);
        }

        [HttpGet]
        public async Task<IActionResult> ManageMatches()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("Role");

            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (role != "Admin")
                return Forbid();

            var matches = await _context.CaseMatches
                .Include(m => m.LostPhoneReport)
                .Include(m => m.FoundPhoneReport)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(matches);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewMatch(
            int matchId,
            string decision,
            string? adminNote)
        {
            var adminId = HttpContext.Session.GetInt32("UserId");

            if (HttpContext.Session.GetString("Role") != "Admin" ||
                adminId == null)
                return Forbid();

            decision = decision?.Trim() ?? "";
            if (decision == "Verified")
            {
                decision = "Confirmed";
            }

            var allowedDecisions = new[]
            {
                "Confirmed",
                "Rejected",
                "Additional Verification Required"
            };

            if (!allowedDecisions.Contains(decision))
            {
                TempData["Error"] = "Invalid match decision.";
                return RedirectToAction(nameof(MatchDetails),
                    new { id = matchId });
            }

            if (decision != "Confirmed" &&
                string.IsNullOrWhiteSpace(adminNote))
            {
                TempData["Error"] =
                    "Please provide a reason or the additional information required.";

                return RedirectToAction(nameof(MatchDetails),
                    new { id = matchId });
            }

            var match = await _context.CaseMatches
                .Include(m => m.LostPhoneReport)
                .Include(m => m.FoundPhoneReport)
                .FirstOrDefaultAsync(m => m.MatchId == matchId);

            if (match == null)
                return NotFound();

            if (match.MatchStatus != "Possible Match" &&
                match.MatchStatus != "Additional Verification Required")
            {
                TempData["Error"] =
                    "This match cannot be reviewed in its current status.";

                return RedirectToAction(nameof(MatchDetails),
                    new { id = matchId });
            }

            if (match.LostPhoneReport == null ||
                match.FoundPhoneReport == null)
                return NotFound();

            match.AdminId = adminId.Value;
            match.ReviewedAt = DateTime.UtcNow;
            match.AdminNote = string.IsNullOrWhiteSpace(adminNote)
                ? null
                : adminNote.Trim();

            match.MatchStatus = decision;

            if (decision == "Confirmed")
            {
                match.LostPhoneReport.Status = "Recovery in Progress";
                match.FoundPhoneReport.Status = "Verified";

                _context.CaseStatusHistories.Add(new CaseStatusHistory
                {
                    CaseId = match.CaseId,
                    Status = "Match Confirmed",
                    ChangedAt = DateTime.UtcNow
                });

                string founderMsg = string.IsNullOrWhiteSpace(adminNote)
                    ? $"Your Found Phone Report #{match.FoundReportId} has been verified and confirmed as a match with Lost Case #{match.CaseId}."
                    : $"Your Found Phone Report #{match.FoundReportId} has been verified and confirmed as a match with Lost Case #{match.CaseId}. Details: {adminNote.Trim()}";

                _context.CaseMessages.Add(new CaseMessage
                {
                    CaseId = match.CaseId,
                    FoundReportId = match.FoundReportId,
                    MatchId = match.MatchId,
                    SenderId = adminId.Value,
                    ReceiverId = match.FoundPhoneReport.UserId,
                    Message = founderMsg,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                });

                string ownerMsg = string.IsNullOrWhiteSpace(adminNote)
                    ? $"A matching device for your Lost Case #{match.CaseId} has been confirmed (Match #{match.MatchId}). Recovery is now in progress."
                    : $"A matching device for your Lost Case #{match.CaseId} has been confirmed (Match #{match.MatchId}). Recovery is now in progress. Details: {adminNote.Trim()}";

                _context.CaseMessages.Add(new CaseMessage
                {
                    CaseId = match.CaseId,
                    FoundReportId = match.FoundReportId,
                    MatchId = match.MatchId,
                    SenderId = adminId.Value,
                    ReceiverId = match.LostPhoneReport.UserId,
                    Message = ownerMsg,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                });
            }
            else if (decision == "Rejected")
            {
                match.FoundPhoneReport.Status = "Rejected";

                _context.CaseStatusHistories.Add(new CaseStatusHistory
                {
                    CaseId = match.CaseId,
                    Status = "Match Rejected",
                    ChangedAt = DateTime.UtcNow
                });

                string founderRejectionMsg = string.IsNullOrWhiteSpace(adminNote)
                    ? $"Your Found Phone Report #{match.FoundReportId} was reviewed and rejected for Lost Case #{match.CaseId}."
                    : $"Your Found Phone Report #{match.FoundReportId} was reviewed and rejected for Lost Case #{match.CaseId}. Reason: {adminNote.Trim()}";

                _context.CaseMessages.Add(new CaseMessage
                {
                    CaseId = match.CaseId,
                    FoundReportId = match.FoundReportId,
                    MatchId = match.MatchId,
                    SenderId = adminId.Value,
                    ReceiverId = match.FoundPhoneReport.UserId,
                    Message = founderRejectionMsg,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                });

                string ownerRejectionMsg = string.IsNullOrWhiteSpace(adminNote)
                    ? $"Candidate Match #{match.MatchId} for your Lost Case #{match.CaseId} was reviewed and rejected."
                    : $"Candidate Match #{match.MatchId} for your Lost Case #{match.CaseId} was reviewed and rejected. Reason: {adminNote.Trim()}";

                _context.CaseMessages.Add(new CaseMessage
                {
                    CaseId = match.CaseId,
                    FoundReportId = match.FoundReportId,
                    MatchId = match.MatchId,
                    SenderId = adminId.Value,
                    ReceiverId = match.LostPhoneReport.UserId,
                    Message = ownerRejectionMsg,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                });

                bool hasOtherActiveMatch = await _context.CaseMatches
                    .AnyAsync(m =>
                        m.CaseId == match.CaseId &&
                        m.MatchId != match.MatchId &&
                        (m.MatchStatus == "Possible Match" ||
                         m.MatchStatus == "Additional Verification Required" ||
                         m.MatchStatus == "Confirmed"));

                if (!hasOtherActiveMatch &&
                    match.LostPhoneReport?.Status == "Match Found")
                {
                    match.LostPhoneReport.Status = "Verified";

                    _context.CaseStatusHistories.Add(new CaseStatusHistory
                    {
                        CaseId = match.CaseId,
                        Status = "Verified",
                        ChangedAt = DateTime.UtcNow
                    });
                }
            }
            else if (decision == "Additional Verification Required")
            {
                string verificationMsg = string.IsNullOrWhiteSpace(adminNote)
                    ? $"Additional verification is required for Match #{match.MatchId} (Lost Case #{match.CaseId} / Found Report #{match.FoundReportId}). Please check your report details and respond with further information."
                    : $"Additional verification is required for Match #{match.MatchId} (Lost Case #{match.CaseId} / Found Report #{match.FoundReportId}): {adminNote.Trim()}";

                _context.CaseMessages.Add(new CaseMessage
                {
                    CaseId = match.CaseId,
                    FoundReportId = match.FoundReportId,
                    MatchId = match.MatchId,
                    SenderId = adminId.Value,
                    ReceiverId = match.FoundPhoneReport.UserId,
                    Message = verificationMsg,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                });

                _context.CaseMessages.Add(new CaseMessage
                {
                    CaseId = match.CaseId,
                    FoundReportId = match.FoundReportId,
                    MatchId = match.MatchId,
                    SenderId = adminId.Value,
                    ReceiverId = match.LostPhoneReport.UserId,
                    Message = verificationMsg,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = decision switch
            {
                "Confirmed" =>
                    "Match confirmed. Both owner and founder have been notified.",
                "Rejected" =>
                    "Match rejected. Both parties have been notified and another found report can be considered.",
                _ =>
                    "Additional information requested. Notifications sent to both parties."
            };

            return RedirectToAction(nameof(MatchDetails),
                new { id = matchId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(
            int matchId,
            string message)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return Forbid();

            var adminId = HttpContext.Session.GetInt32("UserId");
            if (adminId == null)
                return Forbid();

            if (string.IsNullOrWhiteSpace(message) || message.Length > 2000)
            {
                TempData["Error"] = "Message is required (maximum 2000 characters).";
                return RedirectToAction(nameof(MatchDetails),
                    new { id = matchId });
            }

            var match = await _context.CaseMatches
                .Include(m => m.LostPhoneReport)
                .FirstOrDefaultAsync(m => m.MatchId == matchId);

            if (match?.LostPhoneReport == null)
                return NotFound();

            _context.CaseMessages.Add(new CaseMessage
            {
                CaseId = match.CaseId,
                MatchId = match.MatchId,
                SenderId = adminId.Value,
                ReceiverId = match.LostPhoneReport.UserId,
                Message = message.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Message sent to owner.";

            return RedirectToAction(nameof(MatchDetails),
                new { id = matchId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessageToFounder(
            int matchId, string message)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return Forbid();

            int? adminId = HttpContext.Session.GetInt32("UserId");
            if (adminId == null)
                return Forbid();

            if (string.IsNullOrWhiteSpace(message) || message.Length > 2000)
            {
                TempData["Error"] = "Message is required (maximum 2000 characters).";
                return RedirectToAction("MatchDetails", new { id = matchId });
            }

            var match = await _context.CaseMatches
                .Include(m => m.FoundPhoneReport)
                .FirstOrDefaultAsync(m => m.MatchId == matchId);

            if (match == null || match.FoundPhoneReport == null)
                return NotFound();

            var founderId = match.FoundPhoneReport.UserId;

            _context.CaseMessages.Add(new CaseMessage
            {
                CaseId = match.CaseId,
                FoundReportId = match.FoundReportId,
                MatchId = match.MatchId,
                SenderId = adminId.Value,
                ReceiverId = founderId,
                Message = message.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Message sent to the founder.";

            return RedirectToAction("MatchDetails", new { id = matchId });
        }

        public async Task<IActionResult> FoundReportDetails(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return Forbid();

            var report = await _context.FoundPhoneReports
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.FoundReportId == id);

            if (report == null)
                return NotFound();

            var matches = await _context.CaseMatches
                .Include(m => m.LostPhoneReport)
                    .ThenInclude(l => l!.User)
                .Where(m => m.FoundReportId == id)
                .OrderByDescending(m => m.MatchScore)
                .ToListAsync();

            var messages = await _context.CaseMessages
                .Where(m => m.FoundReportId == id)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var currentAdminId = HttpContext.Session.GetInt32("UserId");
            if (currentAdminId != null)
            {
                var unread = messages.Where(m => m.ReceiverId == currentAdminId.Value && !m.IsRead).ToList();
                if (unread.Any())
                {
                    foreach (var m in unread)
                    {
                        m.IsRead = true;
                    }
                    await _context.SaveChangesAsync();
                }
            }

            ViewBag.Matches = matches;
            ViewBag.Messages = messages;

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewFoundReport(
            int foundReportId,
            string decision,
            string? adminNote)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return Forbid();

            var adminId = HttpContext.Session.GetInt32("UserId");
            if (adminId == null)
                return Forbid();

            decision = decision?.Trim() ?? "";
            var allowed = new[] { "Verified", "Rejected", "Additional Information Required" };
            if (!allowed.Contains(decision))
            {
                TempData["Error"] = "Invalid decision.";
                return RedirectToAction(nameof(FoundReportDetails), new { id = foundReportId });
            }

            var report = await _context.FoundPhoneReports
                .FirstOrDefaultAsync(f => f.FoundReportId == foundReportId);

            if (report == null)
                return NotFound();

            if (decision == "Verified")
            {
                report.Status = "Verified";
            }
            else if (decision == "Rejected")
            {
                report.Status = "Rejected";
            }
            else if (decision == "Additional Information Required")
            {
                report.Status = "Under Review";
            }

            string notificationMsg = decision switch
            {
                "Verified" => string.IsNullOrWhiteSpace(adminNote)
                    ? $"Your Found Phone Report #{report.FoundReportId} has been verified and accepted by the administrator."
                    : $"Your Found Phone Report #{report.FoundReportId} has been verified and accepted. Details: {adminNote.Trim()}",
                "Rejected" => string.IsNullOrWhiteSpace(adminNote)
                    ? $"Your Found Phone Report #{report.FoundReportId} has been reviewed and rejected."
                    : $"Your Found Phone Report #{report.FoundReportId} has been reviewed and rejected. Reason: {adminNote.Trim()}",
                _ => string.IsNullOrWhiteSpace(adminNote)
                    ? $"Additional information is required for your Found Phone Report #{report.FoundReportId}."
                    : $"Additional information is required for your Found Phone Report #{report.FoundReportId}: {adminNote.Trim()}"
            };

            _context.CaseMessages.Add(new CaseMessage
            {
                CaseId = 0,
                FoundReportId = report.FoundReportId,
                SenderId = adminId.Value,
                ReceiverId = report.UserId,
                Message = notificationMsg,
                SentAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = decision switch
            {
                "Verified" => "Found report verified and founder notified.",
                "Rejected" => "Found report rejected and founder notified.",
                _ => "Additional information requested from founder."
            };

            return RedirectToAction(nameof(FoundReportDetails), new { id = foundReportId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessageToFounderDirect(
            int foundReportId,
            string message)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return Forbid();

            var adminId = HttpContext.Session.GetInt32("UserId");
            if (adminId == null)
                return Forbid();

            if (string.IsNullOrWhiteSpace(message) || message.Length > 2000)
            {
                TempData["Error"] = "Message is required (maximum 2000 characters).";
                return RedirectToAction(nameof(FoundReportDetails), new { id = foundReportId });
            }

            var report = await _context.FoundPhoneReports
                .FirstOrDefaultAsync(f => f.FoundReportId == foundReportId);

            if (report == null)
                return NotFound();

            _context.CaseMessages.Add(new CaseMessage
            {
                CaseId = 0,
                FoundReportId = report.FoundReportId,
                SenderId = adminId.Value,
                ReceiverId = report.UserId,
                Message = message.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Message sent to founder.";
            return RedirectToAction(nameof(FoundReportDetails), new { id = foundReportId });
        }


    }
}