using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrmLeadTool.Api.Models;

[Table("Product_CRM")]
public class Product
{
    public int ProductId { get; set; }
    public int? CategoryId { get; set; }          // NEW: foreign key to Category
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Product price must be greater than zero.")]
    public decimal Pricing { get; set; }          // Must be > 0
    public string? Features { get; set; }
    public string? Specifications { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Category? Category { get; set; }
    public ICollection<VisitorActivity> Activities { get; set; } = new List<VisitorActivity>();
}