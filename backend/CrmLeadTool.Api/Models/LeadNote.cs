using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("LeadNote_CRM")]
public class LeadNote
{
    public int LeadNoteId { get; set; }
    public int LeadId { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public Lead? Lead { get; set; }
}