using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CrmLeadTool.Api.Models;

[Table("Lead_CRM")]
public class Lead
{
    public int LeadId { get; set; }
    public int? VisitorId { get; set; }
    public int? ProspectId { get; set; }   // Added
    public string CompanyName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ProductIds { get; set; } = "[]";
    public int? Quantity { get; set; }
    public string Timeline { get; set; } = string.Empty;
    public string BusinessRequirement { get; set; } = string.Empty;
    public string? Source { get; set; }
    public DateTime CreatedAt { get; set; }

    public Visitor? Visitor { get; set; }
    public Prospect? Prospect { get; set; }

    public int[] GetProductIdList()
    {
        if (string.IsNullOrEmpty(ProductIds)) return Array.Empty<int>();
        try
        {
            return JsonSerializer.Deserialize<int[]>(ProductIds) ?? Array.Empty<int>();
        }
        catch { return Array.Empty<int>(); }
    }

    public void SetProductIdList(int[] productIds)
    {
        ProductIds = JsonSerializer.Serialize(productIds);
    }
}