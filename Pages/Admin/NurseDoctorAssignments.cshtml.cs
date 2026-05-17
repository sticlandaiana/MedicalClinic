using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MedicalClinic.Data;
using MedicalClinic.Models;
using MedicalClinic.Services;
using System.Security.Claims;

namespace MedicalClinic.Pages.Admin
{
    [Authorize(Roles = "Administrator")]
    public class NurseDoctorAssignmentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _audit;

        public NurseDoctorAssignmentsModel(ApplicationDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public List<NurseDoctorAssignment> Assignments { get; set; }
        public List<Nurse> Nurses { get; set; }
        public List<Doctor> Doctors { get; set; }

        [BindProperty] public int SelectedNurseId { get; set; }
        [BindProperty] public int SelectedDoctorId { get; set; }

        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadDataAsync();

            var exists = await _context.NurseDoctorAssignments
                .AnyAsync(a => a.NurseId == SelectedNurseId && a.DoctorId == SelectedDoctorId);

            if (exists)
            {
                ErrorMessage = "Această asignare există deja.";
                return Page();
            }

            var assignment = new NurseDoctorAssignment
            {
                NurseId = SelectedNurseId,
                DoctorId = SelectedDoctorId
            };
            _context.NurseDoctorAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            // REQ-10: Audit log
            var nurse = Nurses.FirstOrDefault(n => n.NurseId == SelectedNurseId);
            var doctor = Doctors.FirstOrDefault(d => d.DoctorId == SelectedDoctorId);
            await _audit.LogAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.FindFirstValue(ClaimTypes.Email),
                "Create", "NurseDoctorAssignment",
                $"Asistenta {nurse?.Name} asignată la Dr. {doctor?.Name}");

            SuccessMessage = "Asignare adăugată cu succes.";
            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var assignment = await _context.NurseDoctorAssignments
                .Include(a => a.Nurse).Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.NurseDoctorAssignmentId == id);

            if (assignment != null)
            {
                _context.NurseDoctorAssignments.Remove(assignment);
                await _context.SaveChangesAsync();

                await _audit.LogAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    User.FindFirstValue(ClaimTypes.Email),
                    "Delete", "NurseDoctorAssignment",
                    $"Asignare {assignment.Nurse.Name} → Dr. {assignment.Doctor.Name} ștearsă");
            }

            return RedirectToPage();
        }

        private async Task LoadDataAsync()
        {
            Assignments = await _context.NurseDoctorAssignments
                .Include(a => a.Nurse).Include(a => a.Doctor)
                .OrderBy(a => a.Nurse.Name).ToListAsync();
            Nurses = await _context.Nurses.OrderBy(n => n.Name).ToListAsync();
            Doctors = await _context.Doctors.OrderBy(d => d.Name).ToListAsync();
        }
    }
}
