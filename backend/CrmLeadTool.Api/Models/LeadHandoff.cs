using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("LeadHandoff_CRM")]
public class LeadHandoff
{
    public int LeadHandoffId { get; set; }
    public int LeadId { get; set; }
    
    public string Destination { get; set; } = "SALES_CRM"; // SALES_CRM, WEBHOOK, HUBSPOT, SALESFORCE, EXPORT
    public string Status { get; set; } = "PENDING"; // PENDING, SUCCESS, FAILED
    public string? PayloadJson { get; set; }
    public string? ResponseJson { get; set; }
    public string? ErrorMessage { get; set; }
    public int Retries { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? HandedOffAt { get; set; }

    public Lead? Lead { get; set; }
}
