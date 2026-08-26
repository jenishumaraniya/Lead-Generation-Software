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

        var (hash, salt) = PasswordHasher.HashPassword(dto.Password);
        var role = dto.Role.ToUpper() == "ADMIN" ? "ADMIN" : "SALES_REP";

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = hash,
            Salt = salt,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, adminEmail, "CREATE_USER", "User", user.UserId.ToString(), $"Created user: {user.Email} with role {user.Role}");

        return Ok(AuthService.MapUserDto(user));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound(new { error = "User not found." });

        user.FullName = dto.FullName.Trim();
        user.Role = dto.Role.ToUpper() == "ADMIN" ? "ADMIN" : "SALES_REP";
        user.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "ADMIN";
        await _auditLog.LogAsync(null, adminEmail, "UPDATE_USER", "User", user.UserId.ToString(), $"Updated user: {user.Email}");

        return Ok(AuthService.MapUserDto(user));
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
