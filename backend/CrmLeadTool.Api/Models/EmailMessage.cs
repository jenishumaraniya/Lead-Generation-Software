using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("EmailMessage_CRM")]
public class EmailMessage
{
    public int EmailMessageId { get; set; }
    public int CampaignRecipientId { get; set; }
    public int SequenceStepId { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public DateTime SentAt { get; set; }
    public string Status { get; set; } = "SENT";
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public DateTime? RepliedAt { get; set; }

    public CampaignRecipient? CampaignRecipient { get; set; }
    public SequenceStep? SequenceStep { get; set; }
    public ICollection<EmailEvent> Events { get; set; } = new List<EmailEvent>();
}