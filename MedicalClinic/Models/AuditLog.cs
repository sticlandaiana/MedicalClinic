namespace MedicalClinic.Models
{
    /// <summary>REQ-10: Audit log pentru acțiuni critice</summary>
    public class AuditLog
    {
        public int AuditLogId { get; set; }
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
