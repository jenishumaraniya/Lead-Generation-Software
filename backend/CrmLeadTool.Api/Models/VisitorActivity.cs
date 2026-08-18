using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("VisitorActivity_CRM")]
public class VisitorActivity
{
    [Key]
    public long ActivityId { get; set; }
    public int VisitorId { get; set; }
    public int? ProductId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string? PageUrl { get; set; }
    public string? Metadata { get; set; }
    public DateTime Timestamp { get; set; }

    // Navigation
    public Visitor? Visitor { get; set; }
    public Product? Product { get; set; }
}