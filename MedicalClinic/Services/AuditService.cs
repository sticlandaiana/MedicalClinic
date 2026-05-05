using MedicalClinic.Data;
using MedicalClinic.Models;
using Microsoft.EntityFrameworkCore;

namespace MedicalClinic.Services
{
    /// <summary>REQ-10: Audit log - folosește IDbContextFactory pentru a nu contamina contextul principal</summary>
    public class AuditService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<AuditService> _logger;

        public AuditService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<AuditService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task LogAsync(string userId, string userEmail, string action, string entityType, string details = "")
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                context.AuditLogs.Add(new AuditLog
                {
                    UserId     = userId ?? "",
                    UserEmail  = userEmail ?? "",
                    Action     = action,
                    EntityType = entityType,
                    Details    = details,
                    Timestamp  = DateTime.Now
                });
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("AuditService: {Message}", ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
