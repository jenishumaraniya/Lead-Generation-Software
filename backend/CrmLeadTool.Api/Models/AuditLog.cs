namespace CrmLeadTool.Api.Models;

public class AuditLog
{
    public int AuditLogId { get; set; }
    public int? UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // e.g. "CREATE_PRODUCT", "DELETE_USER", "LOGIN"
    public string EntityName { get; set; } = string.Empty; // e.g. "Product", "User", "Category"
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
