using CrmLeadTool.Api.Data;
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
    public async Task<IActionResult> GetProducts()
    {
        // 🔍 TEMPORARY DEBUG: see all products (remove this line later)
        // var all = await _context.Products.ToListAsync();
        // return Ok(all);

        var products = await _context.Products
            .Where(p => p.Status == "ACTIVE")   // Make sure status is exactly "ACTIVE"
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
            return NotFound();

        return Ok(product);
    }
}