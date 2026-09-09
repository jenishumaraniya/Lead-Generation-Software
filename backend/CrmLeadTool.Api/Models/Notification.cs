using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("Notification_CRM")]
public class Notification
{
    [Key]
    public int NotificationId { get; set; }

    public int? UserId { get; set; }

    [MaxLength(50)]
    public string? TargetRole { get; set; } // "ADMIN" or "SALES_REP"

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Type { get; set; } = "GENERAL"; // "LEAD_ASSIGNED", "STATUS_UPDATE", etc.

    public int? LeadId { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }
}
