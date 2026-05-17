using Microsoft.AspNetCore.Identity.UI.Services;

namespace MedicalClinic.Services
{
    /// <summary>
    /// REQ-6: Adapter care conectează IEmailSender (Identity) cu EmailService-ul existent.
    /// Necesar pentru ForgotPassword / ResetPassword din ASP.NET Core Identity.
    /// </summary>
    public class EmailSenderAdapter : IEmailSender
    {
        private readonly EmailService _emailService;

        public EmailSenderAdapter(EmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await _emailService.SendEmailAsync(email, subject, htmlMessage);
        }
    }
}
