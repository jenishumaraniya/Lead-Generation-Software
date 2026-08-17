namespace CrmLeadTool.Api.Models; 
public class VisitorActivity 

{ 
    public long ActivityId { get; set; } 
    public int VisitorId { get; set; } 
    public string ActivityType { get; set; } = string.Empty; 
    public int? ProductId { get; set; } 
    public string? PageUrl { get; set; } 
    public string? Metadata { get; set; } 
    public DateTime Timestamp { get; set; } 
    public Visitor? Visitor { get; set; } 
    public Product? Product { get; set; } 

}