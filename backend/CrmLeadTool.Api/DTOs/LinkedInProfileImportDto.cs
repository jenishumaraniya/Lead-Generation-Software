namespace CrmLeadTool.Api.DTOs;

public class LinkedInProfileImportDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Headline { get; set; }
    public string? JobTitle { get; set; }
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Summary { get; set; }
    public string? Skills { get; set; }
    public string? ExperienceHistory { get; set; }
    public string? Seniority { get; set; }
    public string? Industry { get; set; }
    public string? CompanySize { get; set; }
    public bool AutoCreateLead { get; set; } = true;
    public bool AutoAnalyzeAi { get; set; } = false;
}
