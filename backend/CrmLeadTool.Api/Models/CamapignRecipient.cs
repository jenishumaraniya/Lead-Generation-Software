using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("CampaignRecipient_CRM")]
public class CampaignRecipient
{
    public int CampaignRecipientId { get; set; }
    public int CampaignId { get; set; }
    public int ProspectId { get; set; }
    public string Status { get; set; } = "ENROLLED";
    public int? CurrentStep { get; set; }
    public DateTime EnrolledAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Campaign? Campaign { get; set; }
    public Prospect? Prospect { get; set; }
    public ICollection<EmailMessage> EmailMessages { get; set; } = new List<EmailMessage>();
}