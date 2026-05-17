using MedicalClinic.Data;
using MedicalClinic.Models;
using MedicalClinic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MedicalClinic.Pages.Patients
{
    [Authorize(Roles = "Patient")]
    public class BookAppointmentModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly EmailService _emailService;

        public BookAppointmentModel(ApplicationDbContext context,
            UserManager<IdentityUser> userManager, EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public List<Speciality> Specialities { get; set; }
        public List<ConsultationType> ConsultationTypes { get; set; }
        public List<MedicalClinic.Models.Doctor> Doctors { get; set; }
        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Specialitatea este obligatorie")]
        public int? SelectedSpecialityId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Tipul de consultație este obligatoriu")]
        public int? SelectedConsultationTypeId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Doctorul este obligatoriu")]
        public int? SelectedDoctorId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Data și ora sunt obligatorii")]
        public DateTime? SelectedDateTime { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            Specialities = await _context.Specialities.ToListAsync();
            ConsultationTypes = await _context.ConsultationTypes
                .Include(ct => ct.Speciality)
                .ToListAsync();
            Doctors = await _context.Doctors
                .Include(d => d.DoctorSpecialities)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDataAsync();
                return Page();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

            if (patient == null)
            {
                ErrorMessage = "Profilul de pacient nu a fost găsit.";
                await LoadDataAsync();
                return Page();
            }

            // BR-4: Verifică no-show
            if (patient.NoShowCount >= 3)
            {
                ErrorMessage = "Contul tău a fost suspendat din cauza a 3 absențe nemotivate. Contactează clinica.";
                await LoadDataAsync();
                return Page();
            }

            var consultationType = await _context.ConsultationTypes.FindAsync(SelectedConsultationTypeId);
            var endTime = SelectedDateTime!.Value.AddMinutes(consultationType.Duration);

            // REQ-21: Conflict doctor
            var doctorConflict = await _context.Appointments
                .AnyAsync(a => a.DoctorId == SelectedDoctorId &&
                               a.StartTime < endTime &&
                               a.StartTime.AddMinutes(a.Duration) > SelectedDateTime.Value &&
                               a.Status != "Cancelled");

            if (doctorConflict)
            {
                ErrorMessage = "Doctorul nu este disponibil la ora selectată.";
                await LoadDataAsync();
                return Page();
            }

            // Verificare specialitate doctor
            var doctorHasSpeciality = await _context.DoctorSpecialities
                .AnyAsync(ds => ds.DoctorId == SelectedDoctorId &&
                                ds.SpecialityId == SelectedSpecialityId);

            if (!doctorHasSpeciality)
            {
                ErrorMessage = "Doctorul selectat nu are specialitatea aleasă.";
                await LoadDataAsync();
                return Page();
            }

            // REQ-18 + REQ-29: Găsește sală compatibilă cu specialitatea
            // Verificăm și că sala nu e ocupată la ora respectivă (REQ-20 indirect prin room)
            var room = await _context.Rooms
                .Where(r => r.Status == "Available" &&
                       (r.SpecialityId == null || r.SpecialityId == SelectedSpecialityId) &&
                       !_context.Appointments.Any(a =>
                           a.RoomId == r.RoomId &&
                           a.StartTime < endTime &&
                           a.StartTime.AddMinutes(a.Duration) > SelectedDateTime.Value &&
                           a.Status != "Cancelled"))
                .FirstOrDefaultAsync();

            if (room == null)
            {
                ErrorMessage = "Nu există săli disponibile pentru această specialitate la ora selectată.";
                await LoadDataAsync();
                return Page();
            }

            // REQ-20: Verifică că echipamentele din sală nu sunt rezervate simultan
            var equipmentConflict = await _context.Equipments
                .Where(e => e.RoomId == room.RoomId && e.Status != "Functional")
                .AnyAsync();

            if (equipmentConflict)
            {
                ErrorMessage = "Echipamentele din sala selectată nu sunt funcționale.";
                await LoadDataAsync();
                return Page();
            }

            // REQ-19: Verifică dacă necesită asistentă (parte din BR-5)
            MedicalClinic.Models.Nurse nurse = null;
            if (consultationType.RequiresNurse)
            {
                nurse = await _context.Nurses
                    .Where(n => !_context.Appointments
                        .Any(a => a.NurseId == n.NurseId &&
                                  a.StartTime < endTime &&
                                  a.StartTime.AddMinutes(a.Duration) > SelectedDateTime.Value &&
                                  a.Status != "Cancelled"))
                    .FirstOrDefaultAsync();

                if (nurse == null)
                {
                    // BR-5: toate constrângerile trebuie îndeplinite simultan
                    ErrorMessage = "Nu există asistente disponibile pentru această procedură la ora selectată. " +
                                   "Toate condițiile trebuie îndeplinite simultan (doctor, sală, asistentă).";
                    await LoadDataAsync();
                    return Page();
                }
            }

            var appointment = new Appointment
            {
                PatientId = patient.PatientId,
                DoctorId = SelectedDoctorId!.Value,
                RoomId = room.RoomId,
                ConsultationTypeId = SelectedConsultationTypeId!.Value,
                NurseId = nurse?.NurseId,
                StartTime = SelectedDateTime!.Value,
                Duration = consultationType.Duration,
                Status = "Scheduled",
                CancellationReason = ""
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // REQ-31: Email confirmare
            try
            {
                var doctor = await _context.Doctors.FindAsync(SelectedDoctorId);
                await _emailService.SendAppointmentConfirmationAsync(
                    currentUser.Email,
                    patient.Name,
                    doctor.Name,
                    SelectedDateTime!.Value);
            }
            catch { /* email optional */ }

            SuccessMessage = $"Programarea a fost confirmată pentru {SelectedDateTime:dd/MM/yyyy HH:mm}!";
            await LoadDataAsync();
            return Page();
        }
    }
}
