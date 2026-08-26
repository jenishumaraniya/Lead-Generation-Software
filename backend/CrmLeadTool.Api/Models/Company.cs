using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("Company_CRM")]
public class Company
{
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Industry { get; set; }
    public string? Size { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public CompanyEnrichment? Enrichment { get; set; }
    public ICollection<Prospect> Prospects { get; set; } = new List<Prospect>();
}