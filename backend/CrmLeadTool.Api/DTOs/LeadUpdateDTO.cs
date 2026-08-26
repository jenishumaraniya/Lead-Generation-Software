namespace CrmLeadTool.Api.DTOs;

public class LeadUpdateDto
{
    public string? Status { get; set; }
    public string? Qualification { get; set; }
    public int? Score { get; set; }
}