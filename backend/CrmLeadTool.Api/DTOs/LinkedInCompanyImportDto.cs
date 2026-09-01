namespace CrmLeadTool.Api.DTOs;

public class LinkedInCompanyImportDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string? Tagline { get; set; }
    public string? Industry { get; set; }
    public string? CompanySize { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Description { get; set; }
    public string? PublicSignals { get; set; }
    public bool AutoCreateLead { get; set; } = true;
    public bool AutoAnalyzeAi { get; set; } = false;
}
