using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using MedicalClinic.Data;
using MedicalClinic.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace MedicalClinic.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly AuditService _audit;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            ApplicationDbContext context,
            AuditService audit)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            _audit = audit;
        }

        [BindProperty] public InputModel Input { get; set; }
        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Numele este obligatoriu")]
            [Display(Name = "Nume complet")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "Email-ul este obligatoriu")]
            [EmailAddress(ErrorMessage = "Email invalid")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Parola este obligatorie")]
            [StringLength(100, ErrorMessage = "Parola trebuie să aibă minim {2} caractere.", MinimumLength = 10)]
            [DataType(DataType.Password)]
            [Display(Name = "Parolă")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmă parola")]
            [Compare("Password", ErrorMessage = "Parolele nu coincid.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid) return Page();

            var user = new IdentityUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Patient");

                _context.Patients.Add(new MedicalClinic.Models.Patient
                {
                    UserId = user.Id,
                    Name = Input.FullName,
                    NoShowCount = 0
                });
                await _context.SaveChangesAsync();

                // REQ-10: Audit log înregistrare
                await _audit.LogAsync(user.Id, Input.Email, "Register", "Patient", $"Pacient nou: {Input.FullName}");

                _logger.LogInformation("New patient registered: {Email}", Input.Email);

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToPage("/Patients/Dashboard");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }
    }
}
