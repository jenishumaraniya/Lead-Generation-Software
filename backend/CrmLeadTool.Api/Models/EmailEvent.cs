using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("EmailEvent_CRM")]
public class EmailEvent
{
    public int EmailEventId { get; set; }
    public int EmailMessageId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventTimestamp { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? ClickUrl { get; set; }
    public string? Metadata { get; set; }
    public string? ProviderEventId { get; set; }

    public EmailMessage? EmailMessage { get; set; }
}