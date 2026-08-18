namespace CrmLeadTool.Api.DTOs;

public class ProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Pricing { get; set; }
    public string? Features { get; set; }
    public string? Specifications { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? CategoryId { get; set; }          // only ID, no Category object
    public string? CategoryName { get; set; }     // optional: include category name
}