using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("EnrichmentRun_CRM")]
public class EnrichmentRun
{
    public int EnrichmentRunId { get; set; }
    public int ProspectId { get; set; }
    
    public string Source { get; set; } = "LINKEDIN";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "QUEUED"; // QUEUED, RUNNING, PARTIAL, COMPLETED, NO_DATA, FAILED, BLOCKED
    public string? Error { get; set; }
    public string? RawPayload { get; set; }

    public Prospect? Prospect { get; set; }
    public ICollection<EnrichmentField> Fields { get; set; } = new List<EnrichmentField>();
}
