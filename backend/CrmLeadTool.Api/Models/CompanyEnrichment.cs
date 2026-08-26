using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("CompanyEnrichment_CRM")]
public class CompanyEnrichment
{
    public int CompanyEnrichmentId { get; set; }
    public int CompanyId { get; set; }
    
    public string? Industry { get; set; }
    public string? Size { get; set; }
    public string? Growth { get; set; }
    public string? PublicSignals { get; set; }
    public string? Technologies { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public DateTime? SourceTimestamp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company? Company { get; set; }
}
