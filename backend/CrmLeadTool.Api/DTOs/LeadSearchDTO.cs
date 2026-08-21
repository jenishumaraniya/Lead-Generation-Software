namespace CrmLeadTool.Api.DTOs;

public class LeadSearchDto
{
    public string? Email { get; set; }
    public string? CompanyName { get; set; }
    public string? FullName { get; set; }
    public string? Status { get; set; }
    public string? Qualification { get; set; }
    public int? MinScore { get; set; }
    public int? MaxScore { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
}