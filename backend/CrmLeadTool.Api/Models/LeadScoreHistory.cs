using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("LeadScoreHistory_CRM")]
public class LeadScoreHistory
{
    public int LeadScoreHistoryId { get; set; }
    public int LeadId { get; set; }
    public int? RuleId { get; set; }
    
    public string RuleName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int Delta { get; set; }
    public int TotalScore { get; set; }
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Lead? Lead { get; set; }
    public ScoreRule? Rule { get; set; }
}
