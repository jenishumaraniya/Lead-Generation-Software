namespace CrmLeadTool.Api.DTOs; 

public class CreateActivityDto 

{ 
    public string AnonymousId { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty; 
    public int? ProductId { get; set; } 
    public string? PageUrl { get; set; } 
    public string? Metadata { get; set; } 

} 