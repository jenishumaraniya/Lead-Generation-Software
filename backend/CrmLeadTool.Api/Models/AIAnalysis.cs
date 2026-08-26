using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("AIAnalysis_CRM")]
public class AIAnalysis
{
    public int AIAnalysisId { get; set; }
    public int LeadId { get; set; }
    
    public string Intent { get; set; } = "AWARENESS";
    
    [Column(TypeName = "decimal(5,2)")]  // ✅ Match database column type
    public decimal ConfidenceScore { get; set; }  // ✅ Changed from double to decimal
    
    public string LeadSummary { get; set; } = string.Empty;
    public string PriorityRecommendation { get; set; } = "MEDIUM";
    public string RecommendedNextAction { get; set; } = string.Empty;
    //public string? RecommendedSalesperson { get; set; }
    public string? ProfessionalSummary { get; set; }
    public string? LikelyIndustry { get; set; }
    public string? CompanySize { get; set; }
    public string? LikelyLocation { get; set; }
    public string? PotentialRole { get; set; }
    public string? ModelVersion { get; set; }
    public string? RawResponse { get; set; }
    public DateTime AnalysisDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public Lead? Lead { get; set; }
    public ICollection<AIInsight> Insights { get; set; } = new List<AIInsight>();
}