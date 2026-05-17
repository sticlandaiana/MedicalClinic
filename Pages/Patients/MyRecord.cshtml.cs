using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MedicalClinic.Data;
using MedicalClinic.Models;
using MedicalClinic.Services;
using Microsoft.EntityFrameworkCore;

namespace MedicalClinic.Pages.Patients
{
    [Authorize(Roles = "Patient")]
    public class MyRecordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly MedicalRecordPdfService _pdfService;

        public MyRecordModel(ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            MedicalRecordPdfService pdfService)
        {
            _context = context;
            _userManager = userManager;
            _pdfService = pdfService;
        }

        public List<MedicalRecordEntry> Records { get; set; }
        public List<Allergy> Allergies { get; set; }
        public List<ExternalDocument> ExternalDocuments { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        // REQ-37 + REQ-42: Pacientul poate descărca propria fișă medicală
        public async Task<IActionResult> OnGetExportPdfAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

            if (patient == null)
                return RedirectToPage();

            var records = await _context.MedicalRecordEntries
                .Include(r => r.Appointment)
                .Where(r => r.PatientId == patient.PatientId)
                .OrderByDescending(r => r.Appointment.StartTime)
                .ToListAsync();

            var allergies = await _context.Allergies
                .Where(a => a.PatientId == patient.PatientId)
                .ToListAsync();

            var externalDocs = await _context.ExternalDocuments
                .Where(d => d.PatientId == patient.PatientId)
                .ToListAsync();

            var pdfBytes = _pdfService.GeneratePatientRecordPdf(patient, records, allergies, externalDocs);
            var fileName = $"Fisa_Mea_Medicala_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task LoadDataAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

            if (patient == null)
            {
                Records = new List<MedicalRecordEntry>();
                Allergies = new List<Allergy>();
                ExternalDocuments = new List<ExternalDocument>();
                return;
            }

            Records = await _context.MedicalRecordEntries
                .Include(r => r.Appointment)
                .Include(r => r.Doctor)
                .Where(r => r.PatientId == patient.PatientId)
                .OrderByDescending(r => r.Appointment.StartTime)
                .ToListAsync();

            Allergies = await _context.Allergies
                .Where(a => a.PatientId == patient.PatientId)
                .ToListAsync();

            ExternalDocuments = await _context.ExternalDocuments
                .Where(d => d.PatientId == patient.PatientId)
                .ToListAsync();
        }
    }
}
