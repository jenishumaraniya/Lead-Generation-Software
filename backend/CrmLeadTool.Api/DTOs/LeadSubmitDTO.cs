namespace CrmLeadTool.Api.DTOs;

public class LeadSubmitDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int[] Products { get; set; } = Array.Empty<int>();
    public int[]? ProductIds { get; set; }

    public int[] GetEffectiveProductIds()
    {
        if (ProductIds != null && ProductIds.Length > 0) return ProductIds;
        if (Products != null && Products.Length > 0) return Products;
        return Array.Empty<int>();
    }
    public int? Quantity { get; set; }
    public string Timeline { get; set; } = string.Empty;
    public string BusinessRequirement { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? VisitorId { get; set; }
    public string? ProspectEmail { get; set; }
}