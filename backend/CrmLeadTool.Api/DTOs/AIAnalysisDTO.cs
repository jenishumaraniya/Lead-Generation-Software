namespace CrmLeadTool.Api.DTOs;

public class AIAnalysisResponseDto
{
    public int AIAnalysisId { get; set; }
    public int LeadId { get; set; }
    public string Intent { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }  // ✅ Changed to decimal
    public string LeadSummary { get; set; } = string.Empty;
    public string PriorityRecommendation { get; set; } = string.Empty;
    public string RecommendedNextAction { get; set; } = string.Empty;
   // public string? RecommendedSalesperson { get; set; }
    public string? ProfessionalSummary { get; set; }
    public string? LikelyIndustry { get; set; }
    public string? CompanySize { get; set; }
    public string? LikelyLocation { get; set; }
    public string? PotentialRole { get; set; }
    public DateTime AnalysisDate { get; set; }
    public List<AIInsightDto> Insights { get; set; } = new();
}

public class AIInsightDto
{
    public int AIInsightId { get; set; }
    public string InsightType { get; set; } = string.Empty;
    public string InsightText { get; set; } = string.Empty;
    public decimal? ConfidenceScore { get; set; }  // ✅ Changed to decimal?
    public bool IsAccepted { get; set; }
    public bool IsUsed { get; set; }
}