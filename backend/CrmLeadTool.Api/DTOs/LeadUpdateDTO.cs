namespace CrmLeadTool.Api.DTOs;

public class LeadUpdateDto
{
    public string? Status { get; set; }
    public string? Qualification { get; set; }
    public int? Score { get; set; }
    public int? AssignedTo { get; set; }
    public DateTime? NextFollowUpDate { get; set; }
    public string? Notes { get; set; }
}

public class AddLeadNoteDto
{
    public string NoteText { get; set; } = string.Empty;
}

public class AddLeadActivityDto
{
    public string ActivityType { get; set; } = "NOTE"; // CALL, EMAIL, MEETING, NOTE
    public string Description { get; set; } = string.Empty;
}