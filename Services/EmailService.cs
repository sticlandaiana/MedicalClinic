using MimeKit;
using MailKit.Net.Smtp;

namespace MedicalClinic.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration["Email:From"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _configuration["Email:Host"],
                int.Parse(_configuration["Email:Port"]),
                MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(
                _configuration["Email:Username"],
                _configuration["Email:Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        // REQ-31: Confirmare programare
        public async Task SendAppointmentConfirmationAsync(string toEmail,
            string patientName, string doctorName, DateTime appointmentTime)
        {
            var subject = "Confirmare Programare — MedicalClinic";
            var body = $@"
                <h2>Confirmare Programare</h2>
                <p>Bună ziua, <strong>{patientName}</strong>!</p>
                <p>Programarea ta a fost confirmată cu succes.</p>
                <table>
                    <tr><td><strong>Doctor:</strong></td><td>{doctorName}</td></tr>
                    <tr><td><strong>Data:</strong></td><td>{appointmentTime:dd/MM/yyyy HH:mm}</td></tr>
                </table>
                <p>Vă rugăm să anulați cu cel puțin 24 ore înainte dacă nu puteți ajunge.</p>
                <br/><p>MedicalClinic</p>";
            await SendEmailAsync(toEmail, subject, body);
        }

        // REQ-32: Reminder cu 24h înainte
        public async Task SendAppointmentReminderAsync(string toEmail,
            string patientName, string doctorName, DateTime appointmentTime)
        {
            var subject = "Reminder Programare — MedicalClinic";
            var body = $@"
                <h2>Reminder Programare</h2>
                <p>Bună ziua, <strong>{patientName}</strong>!</p>
                <p>Vă reamintim că mâine aveți o programare.</p>
                <table>
                    <tr><td><strong>Doctor:</strong></td><td>{doctorName}</td></tr>
                    <tr><td><strong>Data:</strong></td><td>{appointmentTime:dd/MM/yyyy HH:mm}</td></tr>
                </table>
                <p>MedicalClinic</p>";
            await SendEmailAsync(toEmail, subject, body);
        }

        // REQ-16: Notificare admin echipament defect
        public async Task SendEquipmentDefectNotificationAsync(string toEmail,
            string equipmentName, string roomName)
        {
            var subject = "⚠️ Echipament Defect — MedicalClinic";
            var body = $@"
                <h2>Alertă Echipament Defect</h2>
                <p>Echipamentul <strong>{equipmentName}</strong> din sala <strong>{roomName}</strong>
                   a fost marcat ca <span style='color:red;font-weight:bold;'>DEFECT</span>.</p>
                <p>Programările afectate au fost flăgate pentru reprogramare.</p>
                <p>Vă rugăm să luați măsuri imediate.</p>
                <br/><p>MedicalClinic — Sistem Automat</p>";
            await SendEmailAsync(toEmail, subject, body);
        }

        // BR-2: Notificare pacient că programarea necesită reprogramare
        public async Task SendReschedulingNotificationAsync(string toEmail,
            string patientName, DateTime appointmentTime, string equipmentName)
        {
            var subject = "Programare Necesită Reprogramare — MedicalClinic";
            var body = $@"
                <h2>Informare Programare</h2>
                <p>Bună ziua, <strong>{patientName}</strong>!</p>
                <p>Din cauza indisponibilității echipamentului <strong>{equipmentName}</strong>,
                   programarea dumneavoastră din <strong>{appointmentTime:dd/MM/yyyy HH:mm}</strong>
                   necesită reprogramare.</p>
                <p>Vă rugăm să contactați clinica sau să faceți o nouă programare online.</p>
                <br/><p>Ne cerem scuze pentru inconveniență.<br/>MedicalClinic</p>";
            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
