using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MobileTracker.Data;
using MobileTracker.Models;
using MobileTracker.Services;
using System.Security.Cryptography;

namespace MobileTracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public AccountController(
            ApplicationDbContext context,
            EmailService emailService,
            IConfiguration configuration)

        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

       
        public IActionResult Signup()
        {
            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(
            User user,
            string ConfirmPassword)
        {
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                ModelState.AddModelError(
                    "PasswordHash",
                    "Password is required.");
            }
            else if (user.PasswordHash.Length < 6)
            {
                ModelState.AddModelError(
                    "PasswordHash",
                    "Password must be at least 6 characters long.");
            }

            if (user.PasswordHash != ConfirmPassword)
            {
                ModelState.AddModelError(
                    "ConfirmPassword",
                    "Passwords do not match.");
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == user.Email.ToLower());

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "An account with this email already exists.");

                return View(user);
            }

            user.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

            bool enableVerification =
                _configuration.GetValue<bool>("EmailSettings:EnableVerification");

            if (enableVerification)
            {
                user.EmailVerificationToken =
                    Convert.ToBase64String(
                        RandomNumberGenerator.GetBytes(32));

                user.EmailConfirmed = false;
            }
            else
            {
                
                user.EmailConfirmed = true;
                user.EmailVerificationToken = null;
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (enableVerification)
            {
                var verificationLink = Url.Action(
                    "VerifyEmail",
                    "Account",
                    new
                    {
                        userId = user.UserId,
                        token = user.EmailVerificationToken
                    },
                    Request.Scheme);

                await _emailService.SendVerificationEmailAsync(
                    user.Email,
                    user.FullName,
                    verificationLink!);

                return RedirectToAction(
                    "VerificationPending",
                    new { email = user.Email });
            }

            return RedirectToAction("Dashboard", "Home");
        }
        
        public IActionResult VerificationPending(string email)
        {
            ViewBag.Email = email;

            return View();
        }

        
        public async Task<IActionResult> VerifyEmail(
            int userId,
            string token)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (user.EmailConfirmed)
            {
                return View("EmailAlreadyVerified");
            }

            if (string.IsNullOrEmpty(user.EmailVerificationToken) ||
                user.EmailVerificationToken != token)
            {
                return View("InvalidVerification");
            }

            user.EmailConfirmed = true;

            user.EmailVerificationToken = null;

            await _context.SaveChangesAsync();

            return View("EmailVerified");
        }

       
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
    string email,
    string password)
        {
            email = email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View();
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError(
                    "",
                    "Please verify your email before logging in.");

                return View();
            }

            bool passwordValid =
                BCrypt.Net.BCrypt.Verify(
                    password,
                    user.PasswordHash);

            if (!passwordValid)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View();
            }

            HttpContext.Session.SetInt32(
                "UserId",
                user.UserId);

            HttpContext.Session.SetString(
                "UserName",
                user.FullName);

            return RedirectToAction(
                "Dashboard",
                "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
    }
}