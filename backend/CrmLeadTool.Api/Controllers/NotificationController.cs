using System.Security.Claims;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly NotificationService _notificationService;

    public NotificationController(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private (string? role, int? userId) ExtractUserClaims()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int? userId = int.TryParse(sub, out int parsed) ? parsed : null;

        if (!string.IsNullOrEmpty(role) && userId.HasValue) return (role, userId);

        var authHeader = Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var tokenString = authHeader.Substring("Bearer ".Length).Trim();
            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(tokenString);
                var tokenRole = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value;
                var tokenSub = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "nameid")?.Value;
                int? tokenUserId = int.TryParse(tokenSub, out int p) ? p : null;
                return (tokenRole, tokenUserId);
            }
            catch {}
        }

        return (null, null);
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int limit = 30)
    {
        var (role, userId) = ExtractUserClaims();
        var notifications = await _notificationService.GetNotificationsAsync(userId, role, limit);
        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var (role, userId) = ExtractUserClaims();
        var count = await _notificationService.GetUnreadCountAsync(userId, role);
        return Ok(new { count });
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var success = await _notificationService.MarkAsReadAsync(id);
        if (!success) return NotFound(new { error = "Notification not found" });
        return Ok(new { message = "Marked as read" });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var (role, userId) = ExtractUserClaims();
        await _notificationService.MarkAllAsReadAsync(userId, role);
        return Ok(new { message = "All notifications marked as read" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var success = await _notificationService.DeleteNotificationAsync(id);
        if (!success) return NotFound(new { error = "Notification not found" });
        return Ok(new { message = "Notification cleared" });
    }

    [HttpDelete("clear-all")]
    public async Task<IActionResult> ClearAll()
    {
        var (role, userId) = ExtractUserClaims();
        await _notificationService.ClearAllNotificationsAsync(userId, role);
        return Ok(new { message = "All notifications cleared" });
    }
}
