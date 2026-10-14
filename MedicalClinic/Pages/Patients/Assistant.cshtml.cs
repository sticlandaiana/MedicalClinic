using System.ComponentModel.DataAnnotations;
using MedicalClinic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace MedicalClinic.Pages.Patients
{
    [Authorize(Roles = "Patient")]
    public class AssistantModel : PageModel
    {
        private const int MaxRequestsPerHour = 10;

        private readonly AiSchedulingAssistant _assistant;
        private readonly AuditService _audit;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IMemoryCache _cache;

        public AssistantModel(
            AiSchedulingAssistant assistant,
            AuditService audit,
            UserManager<IdentityUser> userManager,
            IMemoryCache cache)
        {
            _assistant = assistant;
            _audit = audit;
            _userManager = userManager;
            _cache = cache;
        }

        [BindProperty]
        [Required(ErrorMessage = "Descrie pe scurt simptomele tale.")]
        [StringLength(1000, MinimumLength = 10,
            ErrorMessage = "Descrierea trebuie să aibă între 10 și 1000 de caractere.")]
        public string Symptoms { get; set; } = "";

        public AssistantResult? Result { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync(CancellationToken ct)
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Limită simplă de utilizare (protejează costul API-ului): 10 cereri / oră / utilizator
            var key = $"ai-assistant:{user.Id}";
            var count = _cache.GetOrCreate(key, e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return 0;
            });
            if (count >= MaxRequestsPerHour)
            {
                Result = new AssistantResult
                {
                    Error = "Ai atins limita de cereri pentru asistent. Încearcă din nou peste o oră sau fă programarea manual."
                };
                return Page();
            }
            _cache.Set(key, count + 1, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            Result = await _assistant.AnalyzeAsync(Symptoms, ct);

            // Audit: se loghează doar rezultatul, NU textul simptomelor (date medicale sensibile)
            var details = Result.IsEmergency
                ? "Urgență detectată"
                : Result.HasRecommendation
                    ? $"Recomandare: {Result.SpecialityName} ({Result.Urgency})"
                    : Result.Error != null ? "Eroare asistent" : "Fără recomandare";
            await _audit.LogAsync(user.Id, user.Email ?? "", "AI_ASSISTANT", "Appointment", details);

            return Page();
        }
    }
}
