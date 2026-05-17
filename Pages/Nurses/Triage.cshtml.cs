using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MedicalClinic.Data;
using MedicalClinic.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MedicalClinic.Pages.Nurses
{
    /// <summary>
    /// REQ-40: Permite asistentei să introducă date de triaj (tensiune, greutate, temperatură)
    /// pentru un pacient înainte de consultație. Datele sunt salvate în MedicalRecordEntry.
    /// </summary>
    [Authorize(Roles = "Nurse")]
    public class TriageModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public TriageModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Appointment Appointment { get; set; }
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }
        public bool AlreadyHasTriageData { get; set; }

        [BindProperty]
        public int AppointmentId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Tensiunea arterială este obligatorie (ex: 120/80)")]
        public string BloodPressure { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Greutatea este obligatorie")]
        [Range(1, 500, ErrorMessage = "Greutate invalidă")]
        public double Weight { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Temperatura este obligatorie")]
        [Range(30, 45, ErrorMessage = "Temperatură invalidă")]
        public double Temperature { get; set; }

        public async Task<IActionResult> OnGetAsync(int appointmentId)
        {
            Appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.ConsultationType)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (Appointment == null)
                return RedirectToPage("/Nurses/Appointments");

            AppointmentId = appointmentId;

            // Verifică dacă există deja date de triaj
            var existing = await _context.MedicalRecordEntries
                .FirstOrDefaultAsync(m => m.AppointmentId == appointmentId);

            if (existing != null)
            {
                AlreadyHasTriageData = true;
                BloodPressure = existing.BloodPressure;
                Weight = existing.Weight;
                Temperature = existing.Temperature;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.AppointmentId == AppointmentId);
                return Page();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppointmentId == AppointmentId);

            if (appointment == null)
            {
                ErrorMessage = "Programarea nu a fost găsită.";
                return Page();
            }

            // Verifică dacă există deja un entry — actualizăm doar datele de triaj
            var existing = await _context.MedicalRecordEntries
                .FirstOrDefaultAsync(m => m.AppointmentId == AppointmentId);

            if (existing != null)
            {
                // Actualizează datele de triaj existente
                existing.BloodPressure = BloodPressure;
                existing.Weight = Weight;
                existing.Temperature = Temperature;
            }
            else
            {
                // Creează un entry nou cu doar datele de triaj
                // Diagnosticul va fi completat de doctor ulterior
                _context.MedicalRecordEntries.Add(new MedicalRecordEntry
                {
                    DoctorId = appointment.DoctorId,
                    PatientId = appointment.PatientId,
                    AppointmentId = AppointmentId,
                    BloodPressure = BloodPressure,
                    Weight = Weight,
                    Temperature = Temperature,
                    Diagnoses = "",   // completat de doctor
                    Notes = ""
                });
            }

            await _context.SaveChangesAsync();
            SuccessMessage = "Datele de triaj au fost salvate cu succes!";

            Appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppointmentId == AppointmentId);

            return Page();
        }
    }
}
