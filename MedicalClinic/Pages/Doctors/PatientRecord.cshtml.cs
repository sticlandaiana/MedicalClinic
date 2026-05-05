using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MedicalClinic.Data;
using MedicalClinic.Models;
using MedicalClinic.Services;
using Microsoft.EntityFrameworkCore;

namespace MedicalClinic.Pages.Doctors
{
    [Authorize(Roles = "Doctor")]
    public class PatientRecordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly MedicalRecordPdfService _pdfService;

        public PatientRecordModel(ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment environment,
            MedicalRecordPdfService pdfService)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _pdfService = pdfService;
        }

        public string PatientName { get; set; }
        public int PatientId { get; set; }
        public List<MedicalRecordEntry> Records { get; set; }
        public List<Allergy> Allergies { get; set; }
        public List<ExternalDocument> ExternalDocuments { get; set; }

        public async Task<IActionResult> OnGetAsync(int patientId)
        {
            await LoadDataAsync(patientId);
            return Page();
        }

        private async Task LoadDataAsync(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return;

            PatientId = patientId;
            PatientName = patient.Name;

            Records = await _context.MedicalRecordEntries
                .Include(r => r.Appointment)
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.Appointment.StartTime)
                .ToListAsync();

            Allergies = await _context.Allergies
                .Where(a => a.PatientId == patientId)
                .ToListAsync();

            ExternalDocuments = await _context.ExternalDocuments
                .Where(d => d.PatientId == patientId)
                .ToListAsync();
        }

        // REQ-37 + REQ-42: Export PDF fișă medicală
        public async Task<IActionResult> OnGetExportPdfAsync(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return RedirectToPage(new { patientId });

            var records = await _context.MedicalRecordEntries
                .Include(r => r.Appointment)
                .Where(r => r.PatientId == patientId)
                .OrderByDescending(r => r.Appointment.StartTime)
                .ToListAsync();

            var allergies = await _context.Allergies
                .Where(a => a.PatientId == patientId)
                .ToListAsync();

            var externalDocs = await _context.ExternalDocuments
                .Where(d => d.PatientId == patientId)
                .ToListAsync();

            var pdfBytes = _pdfService.GeneratePatientRecordPdf(patient, records, allergies, externalDocs);

            var fileName = $"Fisa_Medicala_{patient.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        public async Task<IActionResult> OnPostUploadDocumentAsync(int patientId,
            string documentType, IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                var currentUser = await _userManager.GetUserAsync(User);

                _context.ExternalDocuments.Add(new ExternalDocument
                {
                    PatientId = patientId,
                    UserId = currentUser.Id,
                    FilePath = $"/uploads/{fileName}",
                    DocumentType = documentType
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { patientId });
        }

        public async Task<IActionResult> OnPostDeleteDocumentAsync(int docId)
        {
            var doc = await _context.ExternalDocuments.FindAsync(docId);
            if (doc != null)
            {
                var filePath = Path.Combine(_environment.WebRootPath,
                    doc.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);

                _context.ExternalDocuments.Remove(doc);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { patientId = doc?.PatientId });
        }
    }
}
