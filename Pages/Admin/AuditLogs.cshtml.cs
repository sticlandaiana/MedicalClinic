using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MedicalClinic.Data;
using MedicalClinic.Models;

namespace MedicalClinic.Pages.Admin
{
    [Authorize(Roles = "Administrator")]
    public class AuditLogsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public AuditLogsModel(ApplicationDbContext context) => _context = context;

        public List<AuditLog> Logs { get; set; }
        public string FilterAction { get; set; }
        public string FilterEntity { get; set; }

        public async Task OnGetAsync(string action = null, string entity = null)
        {
            FilterAction = action;
            FilterEntity = entity;

            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(action))
                query = query.Where(l => l.Action == action);
            if (!string.IsNullOrEmpty(entity))
                query = query.Where(l => l.EntityType.Contains(entity));

            Logs = await query.OrderByDescending(l => l.Timestamp).Take(500).ToListAsync();
        }
    }
}
