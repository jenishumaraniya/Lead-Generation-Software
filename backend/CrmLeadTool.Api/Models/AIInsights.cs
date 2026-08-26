using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("AIInsight_CRM")]
public class AIInsight
{
    public int AIInsightId { get; set; }
    public int LeadId { get; set; }
    public int? AIAnalysisId { get; set; }
    
    public string InsightType { get; set; } = string.Empty;
    public string InsightText { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(5,2)")]  // ✅ Match database
    public decimal? ConfidenceScore { get; set; }  // ✅ Changed to decimal?
    
    public bool IsAccepted { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }

    public Lead? Lead { get; set; }
    public AIAnalysis? Analysis { get; set; }
}