using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("Product_CRM")]
public class Product
{
    public int ProductId { get; set; }
    public int? CategoryId { get; set; }          // NEW: foreign key to Category
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Pricing { get; set; }          // Now NOT NULL (changed from decimal?)
    public string? Features { get; set; }
    public string? Specifications { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Category? Category { get; set; }
    public ICollection<VisitorActivity> Activities { get; set; } = new List<VisitorActivity>();
}