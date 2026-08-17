namespace CrmLeadTool.Api.Models; 
public class Visitor 

{ 
    public int VisitorId { get; set; } 

 

    public string AnonymousId { get; set; } = string.Empty; 
    public DateTime FirstSeenAt { get; set; } 
    public DateTime LastSeenAt { get; set; } 
    public string ConsentStatus { get; set; } = "UNKNOWN"; 
    public DateTime CreatedAt { get; set; } 
    public ICollection<VisitorActivity> Activities { get; set; } = new List<VisitorActivity>(); 

} 