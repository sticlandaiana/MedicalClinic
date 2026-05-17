using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using MedicalClinic.Data;
using MedicalClinic.Models;
using MedicalClinic.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MedicalClinic.Pages.Admin
{
    [Authorize(Roles = "Administrator")]
    public class EquipmentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditService _audit;

        public EquipmentsModel(ApplicationDbContext context, EmailService emailService,
            UserManager<IdentityUser> userManager, AuditService audit)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
            _audit = audit;
        }

        public List<Equipment> Equipments { get; set; }
        public List<Room> Rooms { get; set; }

        // REQ-17: filter params
        public string FilterStatus { get; set; }
        public int? FilterRoomId { get; set; }

        [BindProperty, Required(ErrorMessage = "Numele este obligatoriu")]
        public string NewEquipmentName { get; set; }

        [BindProperty, Required(ErrorMessage = "Sala este obligatorie")]
        public int? NewEquipmentRoomId { get; set; }

        public async Task OnGetAsync(string filterStatus = null, int? filterRoomId = null)
        {
            FilterStatus = filterStatus;
            FilterRoomId = filterRoomId;
            await LoadDataAsync(filterStatus, filterRoomId);
        }

        private async Task LoadDataAsync(string filterStatus = null, int? filterRoomId = null)
        {
            var query = _context.Equipments.Include(e => e.Room).AsQueryable();
            if (!string.IsNullOrEmpty(filterStatus))
                query = query.Where(e => e.Status == filterStatus);
            if (filterRoomId.HasValue)
                query = query.Where(e => e.RoomId == filterRoomId);
            Equipments = await query.OrderBy(e => e.Name).ToListAsync();
            Rooms = await _context.Rooms.OrderBy(r => r.Name).ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) { await LoadDataAsync(); return Page(); }

            var equipment = new Equipment
            {
                Name = NewEquipmentName,
                RoomId = NewEquipmentRoomId!.Value,
                Status = "Functional"
            };
            _context.Equipments.Add(equipment);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.FindFirstValue(ClaimTypes.Email),
                "Create", "Equipment", $"Echipament '{equipment.Name}' adăugat");

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangeStatusAsync(int id, string status)
        {
            var equipment = await _context.Equipments
                .Include(e => e.Room).FirstOrDefaultAsync(e => e.EquipmentId == id);

            if (equipment != null)
            {
                var previousStatus = equipment.Status;
                equipment.Status = status;
                await _context.SaveChangesAsync();

                await _audit.LogAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
                    User.FindFirstValue(ClaimTypes.Email),
                    "Update", "Equipment",
                    $"Status echipament '{equipment.Name}': {previousStatus} → {status}");

                // REQ-16: email admini la Defect
                if (status == "Defect" && previousStatus != "Defect")
                {
                    var adminUsers = await _userManager.GetUsersInRoleAsync("Administrator");
                    foreach (var admin in adminUsers)
                        try { await _emailService.SendEquipmentDefectNotificationAsync(admin.Email, equipment.Name, equipment.Room.Name); } catch { }
                }

                // BR-2: flăgăm programările afectate
                if (status == "Maintenance" || status == "Defect")
                {
                    var affected = await _context.Appointments
                        .Include(a => a.Patient).ThenInclude(p => p.User)
                        .Where(a => a.RoomId == equipment.RoomId && a.StartTime > DateTime.Now && a.Status == "Scheduled")
                        .ToListAsync();

                    foreach (var appt in affected)
                    {
                        appt.Status = "NeedsRescheduling";
                        try { await _emailService.SendReschedulingNotificationAsync(appt.Patient.User.Email, appt.Patient.Name, appt.StartTime, equipment.Name); } catch { }
                    }
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var equipment = await _context.Equipments.FindAsync(id);
            if (equipment != null)
            {
                await _audit.LogAsync(User.FindFirstValue(ClaimTypes.NameIdentifier),
                    User.FindFirstValue(ClaimTypes.Email),
                    "Delete", "Equipment", $"Echipament '{equipment.Name}' șters");
                _context.Equipments.Remove(equipment);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}
