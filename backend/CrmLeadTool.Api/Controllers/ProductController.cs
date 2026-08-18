using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/product")]
public class ProductController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(int? categoryId = null)
    {
        var query = _context.Products
            .Where(p => p.Status == "ACTIVE");

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await query
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
}