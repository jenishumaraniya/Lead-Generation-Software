using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("Suppression_CRM")]
public class Suppression
{
    public int SuppressionId { get; set; }
    public string Email { get; set; } = string.Empty;
    public int? ProspectId { get; set; }
    
    public string Reason { get; set; } = "OPT_OUT"; // OPT_OUT, BOUNCE, COMPLAINT, MANUAL
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
