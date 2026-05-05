using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MedicalClinic.Data;
using MedicalClinic.Models;
using MedicalClinic.Services;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace MedicalClinic.Pages.Admin
{
    [Authorize(Roles = "Administrator")]
    public class RoomsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _audit;

        public RoomsModel(ApplicationDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        public List<Room> Rooms { get; set; }
        public List<Speciality> Specialities { get; set; }

        [BindProperty, Required] public string NewRoomName { get; set; }
        [BindProperty] public int? NewRoomSpecialityId { get; set; }

        // REQ-17: filter params
        public string FilterStatus { get; set; }
        public int? FilterSpecialityId { get; set; }

        public async Task OnGetAsync(string filterStatus = null, int? filterSpecialityId = null)
        {
            FilterStatus = filterStatus;
            FilterSpecialityId = filterSpecialityId;
            await LoadDataAsync(filterStatus, filterSpecialityId);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) { await LoadDataAsync(); return Page(); }

            var room = new Room
            {
                Name = NewRoomName,
                SpecialityId = NewRoomSpecialityId,
                Status = "Available"
            };
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.FindFirstValue(ClaimTypes.Email),
                "Create", "Room", $"Sală '{room.Name}' adăugată");

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                var oldStatus = room.Status;
                room.Status = room.Status == "Available" ? "Unavailable" : "Available";
                await _context.SaveChangesAsync();

                await _audit.LogAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    User.FindFirstValue(ClaimTypes.Email),
                    "Update", "Room", $"Status sală '{room.Name}': {oldStatus} → {room.Status}");
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();

                await _audit.LogAsync(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    User.FindFirstValue(ClaimTypes.Email),
                    "Delete", "Room", $"Sală '{room.Name}' ștearsă");
            }
            return RedirectToPage();
        }

        private async Task LoadDataAsync(string filterStatus = null, int? filterSpecialityId = null)
        {
            var query = _context.Rooms.Include(r => r.Speciality).AsQueryable();
            if (!string.IsNullOrEmpty(filterStatus))
                query = query.Where(r => r.Status == filterStatus);
            if (filterSpecialityId.HasValue)
                query = query.Where(r => r.SpecialityId == filterSpecialityId);
            Rooms = await query.OrderBy(r => r.Name).ToListAsync();
            Specialities = await _context.Specialities.OrderBy(s => s.Name).ToListAsync();
        }
    }
}
