using System.Security.Claims;
using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/product")]
public class ProductController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuditLogService _auditLog;

    public ProductController(AppDbContext context, AuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] int? categoryId = null, [FromQuery] bool includeInactive = false)
    {
        var query = _context.Products.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.Status == "ACTIVE");
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Pricing = p.Pricing,
                Features = p.Features,
                Specifications = p.Specifications,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.CategoryName : null
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _context.Products
            .Where(p => p.ProductId == id)
            .Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Pricing = p.Pricing,
                Features = p.Features,
                Specifications = p.Specifications,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.CategoryName : null
            })
            .FirstOrDefaultAsync();

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { error = "Product name is required." });
        }

        var product = new Product
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            Pricing = dto.Pricing,
            Features = dto.Features,
            Specifications = dto.Specifications,
            Status = "ACTIVE",
            CategoryId = dto.CategoryId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, userEmail, "CREATE_PRODUCT", "Product", product.ProductId.ToString(), $"Created product: {product.Name} ($ {product.Pricing})");

        return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequestDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound(new { error = "Product not found." });

        product.Name = dto.Name.Trim();
        product.Description = dto.Description;
        product.Pricing = dto.Pricing;
        product.Features = dto.Features;
        product.Specifications = dto.Specifications;
        product.Status = dto.Status ?? "ACTIVE";
        product.CategoryId = dto.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, userEmail, "UPDATE_PRODUCT", "Product", product.ProductId.ToString(), $"Updated product: {product.Name}");

        return Ok(product);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound(new { error = "Product not found." });

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, userEmail, "DELETE_PRODUCT", "Product", id.ToString(), $"Deleted product: {product.Name}");

        return Ok(new { message = "Product deleted successfully." });
    }
}