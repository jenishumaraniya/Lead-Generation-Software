using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("LeadStatusHistory_CRM")]
public class LeadStatusHistory
{
    public int LeadStatusHistoryId { get; set; }
    public int LeadId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
    public string? Reason { get; set; }

    public Lead? Lead { get; set; }
}