using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("SequenceStep_CRM")]
public class SequenceStep
{
    public int SequenceStepId { get; set; }
    public int CampaignId { get; set; }
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int DelayDays { get; set; }
    public int DelayHours { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public Campaign? Campaign { get; set; }
}