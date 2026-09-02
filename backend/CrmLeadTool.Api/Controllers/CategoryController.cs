using System.Security.Claims;
using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Route("api/category")]
public class CategoryController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuditLogService _auditLog;

    public CategoryController(AppDbContext context, AuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.CategoryName)
            .Select(c => new
            {
                c.CategoryId,
                c.CategoryName,
                c.CreatedAt,
                ProductsCount = _context.Products.Count(p => p.CategoryId == c.CategoryId)
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(int id)
    {
        var category = await _context.Categories
            .Where(c => c.CategoryId == id)
            .Select(c => new
            {
                c.CategoryId,
                c.CategoryName,
                c.CreatedAt,
                ProductsCount = _context.Products.Count(p => p.CategoryId == c.CategoryId)
            })
            .FirstOrDefaultAsync();

        if (category == null) return NotFound();
        return Ok(category);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CategoryName))
        {
            return BadRequest(new { error = "Category name is required." });
        }

        var category = new Category
        {
            CategoryName = dto.CategoryName.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, userEmail, "CREATE_CATEGORY", "Category", category.CategoryId.ToString(), $"Created category: {category.CategoryName}");

        return Ok(category);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequestDto dto)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound(new { error = "Category not found." });

        category.CategoryName = dto.CategoryName.Trim();
        await _context.SaveChangesAsync();

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, userEmail, "UPDATE_CATEGORY", "Category", category.CategoryId.ToString(), $"Updated category: {category.CategoryName}");

        return Ok(category);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound(new { error = "Category not found." });

        var productCount = await _context.Products.CountAsync(p => p.CategoryId == id);
        if (productCount > 0)
        {
            return BadRequest(new 
            { 
                error = $"Cannot delete category '{category.CategoryName}' because it contains {productCount} associated product(s). Please reassign or delete the products first." 
            });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, userEmail, "DELETE_CATEGORY", "Category", id.ToString(), $"Deleted category: {category.CategoryName}");

        return Ok(new { message = "Category deleted successfully." });
    }
}
