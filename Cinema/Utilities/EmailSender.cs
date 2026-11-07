using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace Cinema.Utilities
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    "shahd.abdelrahman.eelu@gmail.com",
                    "YOUR_APP_PASSWORD")
            };

            var message = new MailMessage(
                "shahd.abdelrahman.eelu@gmail.com",
                email,
                subject,
                htmlMessage)
            {
                IsBodyHtml = true
            };

            return client.SendMailAsync(message);
        }
    }
}