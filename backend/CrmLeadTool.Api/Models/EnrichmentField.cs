using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("EnrichmentField_CRM")]
public class EnrichmentField
{
    public int EnrichmentFieldId { get; set; }
    public int EnrichmentRunId { get; set; }
    
    public string FieldName { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string Source { get; set; } = "LINKEDIN";
    public string Confidence { get; set; } = "HIGH"; // HIGH, MEDIUM, LOW
    public bool IsAiInferred { get; set; } = false; // Distinguish verified vs AI inferred
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public EnrichmentRun? EnrichmentRun { get; set; }
}
