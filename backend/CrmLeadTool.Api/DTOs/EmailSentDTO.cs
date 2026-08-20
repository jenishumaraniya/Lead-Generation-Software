namespace CrmLeadTool.Api.DTOs;

public class EmailSendDto
{
    public int CampaignRecipientId { get; set; }
    public string? FromEmail { get; set; }
}