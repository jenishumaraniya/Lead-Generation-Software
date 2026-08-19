using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("Visitor_CRM")]
public class Visitor
{
    public int VisitorId { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]   // <-- important!
    public Guid PublicId { get; set; }

    public string AnonymousId { get; set; } = string.Empty;
    public string ConsentStatus { get; set; } = "UNKNOWN";
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }

    public ICollection<VisitorActivity> Activities { get; set; } = new List<VisitorActivity>();
}