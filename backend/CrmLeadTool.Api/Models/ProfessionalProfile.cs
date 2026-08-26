using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("ProfessionalProfile_CRM")]
public class ProfessionalProfile
{
    public int ProfessionalProfileId { get; set; }
    public int ProspectId { get; set; }
    
    public string? LinkedInReference { get; set; }
    public string? Title { get; set; }
    public string? Seniority { get; set; }
    public string? Function { get; set; }
    public string? Location { get; set; }
    public string? Summary { get; set; }
    public string? Skills { get; set; }
    public string? ExperienceYears { get; set; }
    public DateTime? SourceTimestamp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Prospect? Prospect { get; set; }
}
