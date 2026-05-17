using System.ComponentModel.DataAnnotations;
using MedicalClinic.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MedicalClinic.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly AuditService _audit;

        public LoginModel(SignInManager<IdentityUser> signInManager,
            ILogger<LoginModel> logger, AuditService audit)
        {
            _signInManager = signInManager;
            _logger = logger;
            _audit = audit;
        }

        [BindProperty] public InputModel Input { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        public string ReturnUrl { get; set; }
        [TempData] public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email-ul este obligatoriu")]
            [EmailAddress(ErrorMessage = "Email invalid")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Parola este obligatorie")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Ține-mă conectat")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
                ModelState.AddModelError(string.Empty, ErrorMessage);
            returnUrl ??= Url.Content("~/");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (!ModelState.IsValid) return Page();

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in: {Email}", Input.Email);

                var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);

                // REQ-10: Audit log (fault-tolerant - nu blochează login-ul)
                await _audit.LogAsync(user.Id, Input.Email, "Login", "User", "Autentificare reușită");

                var roles = await _signInManager.UserManager.GetRolesAsync(user);

                if (roles.Contains("Administrator"))
                    return RedirectToPage("/Admin/Dashboard");
                else if (roles.Contains("Doctor"))
                    return RedirectToPage("/Doctors/Dashboard");
                else if (roles.Contains("Nurse"))
                    return RedirectToPage("/Nurses/Dashboard");
                else if (roles.Contains("Patient"))
                    return RedirectToPage("/Patients/Dashboard");
                else
                    return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out: {Email}", Input.Email);
                await _audit.LogAsync("", Input.Email, "Lockout", "User", "Cont blocat");
                return RedirectToPage("./Lockout");
            }

            ModelState.AddModelError(string.Empty, "Email sau parolă incorectă.");
            return Page();
        }
    }
}
