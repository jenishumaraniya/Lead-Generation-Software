using System.Security.Claims;
using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using CrmLeadTool.Api.Services;
using CrmLeadTool.Api.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/users")]
[Route("api/employee")]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly AuditLogService _auditLog;

    public UserController(AppDbContext context, AuthService authService, AuditLogService auditLog)
    {
        _context = context;
        _authService = authService;
        _auditLog = auditLog;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .Include(u => u.Category)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => AuthService.MapUserDto(u))
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { error = "Email and Password are required." });
        }

        var normalizedEmail = dto.Email.Trim().ToLower();
        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
        {
            return BadRequest(new { error = "A user with this email address already exists." });
        }

        if (dto.Role?.ToUpper() == "ADMIN")
        {
            var adminExists = await _context.Users.AnyAsync(u => u.Role == "ADMIN");
            if (adminExists)
            {
                return BadRequest(new { error = "Only one Administrator is permitted in the platform. You can only add Sales Representatives." });
            }
        }

        // Validate 1-to-1 Category assignment: No two sales persons can be in the same category
        if (dto.CategoryId.HasValue && dto.CategoryId.Value > 0)
        {
            var existingRep = await _context.Users
                .Include(u => u.Category)
                .FirstOrDefaultAsync(u => u.CategoryId == dto.CategoryId.Value && u.Role == "SALES_REP" && u.IsActive);

            if (existingRep != null)
            {
                var categoryName = existingRep.Category?.CategoryName ?? $"Category #{dto.CategoryId.Value}";
                return BadRequest(new { error = $"Category '{categoryName}' is already assigned to salesperson '{existingRep.FullName}'. No two sales representatives can be assigned to the same category." });
            }
        }

        var (hash, salt) = PasswordHasher.HashPassword(dto.Password);
        var role = "SALES_REP"; // Always create as SALES_REP unless first setup

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = hash,
            Salt = salt,
            Role = role,
            IsActive = true,
            CategoryId = (dto.CategoryId.HasValue && dto.CategoryId.Value > 0) ? dto.CategoryId.Value : null,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var created = await _context.Users.Include(u => u.Category).FirstOrDefaultAsync(u => u.UserId == user.UserId);

        var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, adminEmail, "CREATE_USER", "User", user.UserId.ToString(), $"Created sales representative: {user.Email} (Category: {created?.Category?.CategoryName ?? "None"})");

        return Ok(AuthService.MapUserDto(created ?? user));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await _context.Users.Include(u => u.Category).FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return NotFound(new { error = "User not found." });

        // Validate 1-to-1 Category assignment: No two sales persons can be in the same category
        if (dto.CategoryId.HasValue && dto.CategoryId.Value > 0)
        {
            var existingRep = await _context.Users
                .Include(u => u.Category)
                .FirstOrDefaultAsync(u => u.UserId != id && u.CategoryId == dto.CategoryId.Value && u.Role == "SALES_REP" && u.IsActive);

            if (existingRep != null)
            {
                var categoryName = existingRep.Category?.CategoryName ?? $"Category #{dto.CategoryId.Value}";
                return BadRequest(new { error = $"Category '{categoryName}' is already assigned to salesperson '{existingRep.FullName}'. No two sales representatives can be assigned to the same category." });
            }

            user.CategoryId = dto.CategoryId.Value;
        }
        else
        {
            user.CategoryId = null;
        }

        user.FullName = dto.FullName.Trim();
        user.Role = dto.Role.ToUpper() == "ADMIN" ? "ADMIN" : "SALES_REP";
        user.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        var updated = await _context.Users.Include(u => u.Category).FirstOrDefaultAsync(u => u.UserId == id);

        var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, adminEmail, "UPDATE_USER", "User", user.UserId.ToString(), $"Updated user: {user.Email} (Category: {updated?.Category?.CategoryName ?? "None"})");

        return Ok(AuthService.MapUserDto(updated ?? user));
    }

    [HttpPut("{id}/category")]
    public async Task<IActionResult> AssignCategory(int id, [FromBody] AssignCategoryDto dto)
    {
        var user = await _context.Users.Include(u => u.Category).FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return NotFound(new { error = "User not found." });

        if (dto.CategoryId.HasValue && dto.CategoryId.Value > 0)
        {
            var existingRep = await _context.Users
                .Include(u => u.Category)
                .FirstOrDefaultAsync(u => u.UserId != id && u.CategoryId == dto.CategoryId.Value && u.Role == "SALES_REP" && u.IsActive);

            if (existingRep != null)
            {
                var categoryName = existingRep.Category?.CategoryName ?? $"Category #{dto.CategoryId.Value}";
                return BadRequest(new { error = $"Category '{categoryName}' is already assigned to salesperson '{existingRep.FullName}'. No two sales representatives can be assigned to the same category." });
            }

            user.CategoryId = dto.CategoryId.Value;
        }
        else
        {
            user.CategoryId = null;
        }

        await _context.SaveChangesAsync();

        // Auto-assign existing unassigned single-category leads of this category to this sales rep
        if (user.CategoryId.HasValue && user.Role == "SALES_REP")
        {
            var catId = user.CategoryId.Value;
            var catProductIds = await _context.Products.Where(p => p.CategoryId == catId).Select(p => p.ProductId).ToListAsync();
            var unassignedSingleLeads = await _context.Leads.Where(l => !l.IsMultiCategory && !l.AssignedTo.HasValue).ToListAsync();
            
            foreach (var l in unassignedSingleLeads)
            {
                var leadProductIds = l.GetProductIdList();
                if (leadProductIds.Any(pid => catProductIds.Contains(pid)))
                {
                    l.AssignedTo = user.UserId;
                    _context.LeadActivities.Add(new LeadActivity
                    {
                        LeadId = l.LeadId,
                        ActivityType = "AUTO_ASSIGNMENT",
                        Description = $"Lead automatically assigned to '{user.FullName}' upon category assignment.",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "SYSTEM"
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        var updated = await _context.Users.Include(u => u.Category).FirstOrDefaultAsync(u => u.UserId == id);

        var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, adminEmail, "ASSIGN_CATEGORY", "User", user.UserId.ToString(), $"Assigned category: {updated?.Category?.CategoryName ?? "None"} to {user.Email}");

        return Ok(AuthService.MapUserDto(updated ?? user));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound(new { error = "User not found." });

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, adminEmail, "DELETE_USER", "User", id.ToString(), $"Deleted user: {user.Email}");

        return Ok(new { message = "User deleted successfully." });
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] AdminResetPasswordDto dto)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        int adminId = int.TryParse(sub, out int parsed) ? parsed : 1;

        try
        {
            await _authService.AdminResetPasswordAsync(adminId, id, dto.NewPassword);
            return Ok(new { message = "Password reset successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/toggle-status")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound(new { error = "User not found." });

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();

        var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, adminEmail, "TOGGLE_USER_STATUS", "User", user.UserId.ToString(), $"User status changed to {(user.IsActive ? "Active" : "Deactivated")}");

        return Ok(AuthService.MapUserDto(user));
    }
}
