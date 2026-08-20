using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("Campaign_CRM")]
public class Campaign
{
    public int CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "DRAFT";
    public DateTime? ScheduleStartDate { get; set; }
    public DateTime? ScheduleEndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<SequenceStep> Steps { get; set; } = new List<SequenceStep>();
    public ICollection<CampaignRecipient> Recipients { get; set; } = new List<CampaignRecipient>();
}