using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MedicalClinic.Data;
using MedicalClinic.Models;
using System.Text.Json;

namespace MedicalClinic.Pages.Shared
{
    [Authorize]
    public class CalendarModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public CalendarModel(ApplicationDbContext context) => _context = context;

        public List<Doctor> Doctors { get; set; } = new();
        public List<Room> Rooms { get; set; } = new();
        public List<Speciality> Specialities { get; set; } = new();
        public string AppointmentsJson { get; set; } = "[]";

        // REQ-24: Filter params
        public int? FilterDoctorId { get; set; }
        public int? FilterRoomId { get; set; }
        public int? FilterSpecialityId { get; set; }

        public async Task OnGetAsync(int? doctorId, int? roomId, int? specialityId)
        {
            FilterDoctorId = doctorId;
            FilterRoomId = roomId;
            FilterSpecialityId = specialityId;

            Doctors = await _context.Doctors.OrderBy(d => d.Name).ToListAsync();
            Rooms = await _context.Rooms.OrderBy(r => r.Name).ToListAsync();
            Specialities = await _context.Specialities.OrderBy(s => s.Name).ToListAsync();

            // Build query – scope by role
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Room)
                .Include(a => a.ConsultationType).ThenInclude(ct => ct.Speciality)
                .AsQueryable();

            // If current user is a doctor, show only their appointments
            if (User.IsInRole("Doctor"))
            {
                var doctor = await _context.Doctors
                    .FirstOrDefaultAsync(d => d.User.Email == User.Identity.Name);
                if (doctor != null)
                    query = query.Where(a => a.DoctorId == doctor.DoctorId);
            }
            // If patient, show only their appointments
            else if (User.IsInRole("Patient"))
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.User.Email == User.Identity.Name);
                if (patient != null)
                    query = query.Where(a => a.PatientId == patient.PatientId);
            }

            // REQ-24: Apply filters
            if (doctorId.HasValue)
                query = query.Where(a => a.DoctorId == doctorId);
            if (roomId.HasValue)
                query = query.Where(a => a.RoomId == roomId);
            if (specialityId.HasValue)
                query = query.Where(a => a.ConsultationType.SpecialityId == specialityId);

            var appointments = await query
                .Where(a => a.Status != "Cancelled")
                .OrderBy(a => a.StartTime)
                .ToListAsync();

            // Serialize for FullCalendar
            var events = appointments.Select(a => new
            {
                id = a.AppointmentId,
                title = $"{a.Patient.Name} – {a.Doctor.Name}",
                start = a.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = a.StartTime.AddMinutes(a.Duration > 0 ? a.Duration : 30).ToString("yyyy-MM-ddTHH:mm:ss"),
                extendedProps = new
                {
                    patient = a.Patient.Name,
                    doctor = a.Doctor.Name,
                    room = a.Room?.Name ?? "-",
                    status = a.Status,
                    speciality = a.ConsultationType?.Speciality?.Name ?? ""
                }
            });

            AppointmentsJson = JsonSerializer.Serialize(events);
        }
    }
}
