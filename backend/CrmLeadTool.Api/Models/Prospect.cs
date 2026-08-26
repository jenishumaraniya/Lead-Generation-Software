using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("Prospect_CRM")]
public class Prospect
{
    public int ProspectId { get; set; }
    public int? CompanyId { get; set; }
    
    [Column("PublicId")]
    public Guid? PublicId { get; set; }
    
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? LinkedInUrl { get; set; }
    public string Source { get; set; } = "MANUAL";
    public string Status { get; set; } = "NEW";
    public int? Score { get; set; }
    public string? Qualification { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Company? Company { get; set; }
    public Visitor? Visitor { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }
    public ICollection<EnrichmentRun> EnrichmentRuns { get; set; } = new List<EnrichmentRun>();
    public ICollection<CampaignRecipient> CampaignRecipients { get; set; } = new List<CampaignRecipient>();
    public ICollection<Lead> Leads { get; set; } = new List<Lead>();
}