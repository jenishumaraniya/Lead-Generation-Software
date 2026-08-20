namespace CrmLeadTool.Api.DTOs;

public class MailtrapWebhookDto
{
    public string Event { get; set; } = string.Empty;  // "open", "click", "bounce", etc.
    public MailtrapMessage Message { get; set; } = new();
    public string Recipient { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? User_Agent { get; set; }
    public string? Ip { get; set; }
    public string? Url { get; set; }
}

public class MailtrapMessage
{
    public string Message_Id { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}