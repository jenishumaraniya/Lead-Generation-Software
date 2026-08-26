using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("AIAnalysisHistory_CRM")]
public class AIAnalysisHistory
{
    public int AIAnalysisHistoryId { get; set; }
    public int LeadId { get; set; }
    
    public string? PreviousIntent { get; set; }
    public string NewIntent { get; set; } = string.Empty;
    public string? PreviousPriority { get; set; }
    public string NewPriority { get; set; } = string.Empty;
    
    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }  // ✅ Changed from UserId to string
    public string? Reason { get; set; }

    public Lead? Lead { get; set; }
}