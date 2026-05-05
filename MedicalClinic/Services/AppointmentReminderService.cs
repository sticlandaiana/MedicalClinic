using MedicalClinic.Data;
using Microsoft.EntityFrameworkCore;

namespace MedicalClinic.Services
{
    /// <summary>
    /// REQ-32: Serviciu de background care trimite reminder-uri cu 24h înainte de programare.
    /// Rulează o dată pe oră și verifică programările din ziua următoare.
    /// </summary>
    public class AppointmentReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AppointmentReminderService> _logger;

        public AppointmentReminderService(IServiceScopeFactory scopeFactory,
            ILogger<AppointmentReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AppointmentReminderService pornit.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Eroare la trimiterea reminder-urilor.");
                }

                // Verifică la fiecare oră
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task SendRemindersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

            // Fereastra: programări care încep între 23h și 25h de acum
            var from = DateTime.Now.AddHours(23);
            var to = DateTime.Now.AddHours(25);

            var appointments = await context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor)
                .Where(a => a.StartTime >= from &&
                            a.StartTime <= to &&
                            a.Status == "Scheduled" &&
                            !a.ReminderSent)
                .ToListAsync();

            foreach (var appt in appointments)
            {
                try
                {
                    await emailService.SendAppointmentReminderAsync(
                        appt.Patient.User.Email,
                        appt.Patient.Name,
                        appt.Doctor.Name,
                        appt.StartTime);

                    appt.ReminderSent = true;
                    _logger.LogInformation(
                        "Reminder trimis pentru programarea {Id} — pacient {Name}",
                        appt.AppointmentId, appt.Patient.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Nu s-a putut trimite reminder pentru programarea {Id}", appt.AppointmentId);
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
