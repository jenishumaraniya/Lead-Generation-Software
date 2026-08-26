namespace CrmLeadTool.Api.DTOs;

public class EmailEventWebhookDto
{
    public string EventType { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? ClickUrl { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? Timestamp { get; set; }
    public string? ProviderEventId { get; set; }
    public string? Metadata { get; set; }
}