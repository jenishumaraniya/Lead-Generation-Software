using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("LeadActivity_CRM")]
public class LeadActivity
{
    public int LeadActivityId { get; set; }
    public int LeadId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public Lead? Lead { get; set; }
}