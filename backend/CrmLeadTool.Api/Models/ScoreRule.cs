using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("ScoreRule_CRM")]
public class ScoreRule
{
    public int ScoreRuleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // PRODUCT_VIEW, REPEAT_VISIT, ROLE_MATCH, COMPANY_FIT, LINKEDIN_ENRICHED, FORM_SUBMIT, EMAIL_OPEN, EMAIL_CLICK, EMAIL_REPLY, EMAIL_BOUNCE, IRRELEVANT_REQ, INVALID_CONTACT
    public string Category { get; set; } = "ENGAGEMENT"; // ENGAGEMENT, FIT, INTENT, ENRICHMENT, COMPLIANCE
    public string Direction { get; set; } = "POSITIVE"; // POSITIVE, NEGATIVE
    public int Points { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
