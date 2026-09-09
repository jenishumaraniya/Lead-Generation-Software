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
[Route("api/products")]
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
    public async Task<IActionResult> GetProducts([FromQuery] int? categoryId = null, [FromQuery] bool includeInactive = true)
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
                ImageUrl = p.ImageUrl,
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
                ImageUrl = p.ImageUrl,
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

        if (dto.Pricing <= 0)
        {
            return BadRequest(new { error = "Product price must be greater than zero (cannot be 0 or negative)." });
        }

        var product = new Product
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            Pricing = dto.Pricing,
            Features = dto.Features,
            Specifications = dto.Specifications,
            Status = string.IsNullOrWhiteSpace(dto.Status) ? "DRAFT" : dto.Status.Trim().ToUpper(),
            ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim(),
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
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { error = "Product name is required." });
        }

        if (dto.Pricing <= 0)
        {
            return BadRequest(new { error = "Product price must be greater than zero (cannot be 0 or negative)." });
        }

        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound(new { error = "Product not found." });

        product.Name = dto.Name.Trim();
        product.Description = dto.Description;
        product.Pricing = dto.Pricing;
        product.Features = dto.Features;
        product.Specifications = dto.Specifications;
        product.Status = string.IsNullOrWhiteSpace(dto.Status) ? product.Status : dto.Status.Trim().ToUpper();
        product.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        product.CategoryId = dto.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, userEmail, "UPDATE_PRODUCT", "Product", product.ProductId.ToString(), $"Updated product: {product.Name}");

        return Ok(product);
    }

    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProductImage([FromForm] IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            if (Request.HasFormContentType && Request.Form.Files.Count > 0)
            {
                file = Request.Form.Files[0];
            }
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No image file was uploaded. Please select a valid image file." });
        }

        if (file.Length > 15 * 1024 * 1024)
        {
            return BadRequest(new { error = "Image file size exceeds the 15MB limit." });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg", ".gif", ".bmp", ".ico", ".avif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
        {
            ext = ".png"; // Default fallback extension if not provided
        }

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", "products");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var uniqueFileName = $"prod_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/products/{uniqueFileName}";
        return Ok(new { imageUrl = relativeUrl, message = "Image uploaded successfully." });
    }

    [HttpGet("image/{fileName}")]
    public IActionResult GetProductImageFile(string fileName)
    {
        var sanitized = Path.GetFileName(fileName);
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products", sanitized);
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { error = "Image not found." });
        }

        var ext = Path.GetExtension(sanitized).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".gif" => "image/gif",
            ".avif" => "image/avif",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };

        return PhysicalFile(filePath, contentType);
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

    [HttpPost("{id}/status")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateProductStatus(int id, [FromBody] ChangeProductStatusRequestDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound(new { error = "Product not found." });

        product.Status = string.IsNullOrWhiteSpace(dto?.Status) ? "ACTIVE" : dto.Status.Trim().ToUpper();
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, userEmail, "UPDATE_PRODUCT_STATUS", "Product", product.ProductId.ToString(), $"Changed product '{product.Name}' status to {product.Status}");

        return Ok(product);
    }
}

public class ChangeProductStatusRequestDto
{
    public string Status { get; set; } = "ACTIVE";
}