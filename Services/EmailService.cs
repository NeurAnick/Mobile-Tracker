using System.Net;
using System.Net.Mail;

namespace MobileTracker.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendVerificationEmailAsync(
            string recipientEmail,
            string recipientName,
            string verificationLink)
        {
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderName = _configuration["EmailSettings:SenderName"];
            var appPassword = _configuration["EmailSettings:AppPassword"];

            using var message = new MailMessage();

            message.From = new MailAddress(senderEmail!, senderName);
            message.To.Add(recipientEmail);

            message.Subject = "Mobile Tracker - Verify Your Email";

            message.Body = $@"
Hello {recipientName},

Welcome to Mobile Tracker!

Please verify your email address by clicking the link below:

{verificationLink}

If you did not create this account, you can ignore this email.

Regards,
Mobile Tracker Team
";

            message.IsBodyHtml = false;

            using var smtp = new SmtpClient("smtp.gmail.com", 587);

            smtp.EnableSsl = true;
            smtp.Credentials = new NetworkCredential(
                senderEmail,
                appPassword
            );

            await smtp.SendMailAsync(message);
        }
    }
}