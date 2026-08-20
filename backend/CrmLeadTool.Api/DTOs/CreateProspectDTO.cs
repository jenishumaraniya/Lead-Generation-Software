namespace CrmLeadTool.Api.DTOs;

public class CreateProspectDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyDomain { get; set; }
    public string? Industry { get; set; }
    public string? Source { get; set; }
    public string? VisitorId { get; set; } // AnonymousId from Visitor_CRM
}