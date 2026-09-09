using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Services;

public class NotificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Notification> CreateNotificationAsync(
        string title, 
        string message, 
        string type, 
        string? targetRole = null, 
        int? userId = null, 
        int? leadId = null)
    {
        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = type,
            TargetRole = targetRole,
            UserId = userId,
            LeadId = leadId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Notification created: '{Title}' for Role: {Role}, User: {UserId}", title, targetRole, userId);
        return notification;
    }

    public async Task<List<Notification>> GetNotificationsAsync(int? userId, string? role, int limit = 30)
    {
        var query = _context.Notifications.AsQueryable();

        if (string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(n => n.TargetRole == "ADMIN" || (userId.HasValue && n.UserId == userId.Value));
        }
        else if (string.Equals(role, "SALES_REP", StringComparison.OrdinalIgnoreCase) || string.Equals(role, "SALES", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(n => (userId.HasValue && n.UserId == userId.Value) || (n.TargetRole == "SALES_REP" && !n.UserId.HasValue));
        }
        else if (userId.HasValue)
        {
            query = query.Where(n => n.UserId == userId.Value);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(int? userId, string? role)
    {
        var query = _context.Notifications.Where(n => !n.IsRead);

        if (string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(n => n.TargetRole == "ADMIN" || (userId.HasValue && n.UserId == userId.Value));
        }
        else if (string.Equals(role, "SALES_REP", StringComparison.OrdinalIgnoreCase) || string.Equals(role, "SALES", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(n => (userId.HasValue && n.UserId == userId.Value) || (n.TargetRole == "SALES_REP" && !n.UserId.HasValue));
        }
        else if (userId.HasValue)
        {
            query = query.Where(n => n.UserId == userId.Value);
        }

        return await query.CountAsync();
    }

    public async Task<bool> MarkAsReadAsync(int notificationId)
    {
        var item = await _context.Notifications.FindAsync(notificationId);
        if (item == null) return false;

        item.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(int? userId, string? role)
    {
        var query = _context.Notifications.Where(n => !n.IsRead);

        if (string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(n => n.TargetRole == "ADMIN" || (userId.HasValue && n.UserId == userId.Value));
        }
        else if (string.Equals(role, "SALES_REP", StringComparison.OrdinalIgnoreCase) || string.Equals(role, "SALES", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(n => (userId.HasValue && n.UserId == userId.Value) || (n.TargetRole == "SALES_REP" && !n.UserId.HasValue));
        }
        else if (userId.HasValue)
        {
            query = query.Where(n => n.UserId == userId.Value);
        }

        var unreadItems = await query.ToListAsync();
        foreach (var item in unreadItems)
        {
            item.IsRead = true;
        }

        if (unreadItems.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId)
    {
        var item = await _context.Notifications.FindAsync(notificationId);
        if (item == null) return false;

        _context.Notifications.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task ClearAllNotificationsAsync(int? userId, string? role)
    {
        var query = _context.Notifications.AsQueryable();

        if (string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(n => n.TargetRole == "ADMIN" || (userId.HasValue && n.UserId == userId.Value));
        }
        else if (string.Equals(role, "SALES_REP", StringComparison.OrdinalIgnoreCase) || string.Equals(role, "SALES", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(n => (userId.HasValue && n.UserId == userId.Value) || (n.TargetRole == "SALES_REP" && !n.UserId.HasValue));
        }
        else if (userId.HasValue)
        {
            query = query.Where(n => n.UserId == userId.Value);
        }

        var items = await query.ToListAsync();
        if (items.Count > 0)
        {
            _context.Notifications.RemoveRange(items);
            await _context.SaveChangesAsync();
        }
    }
}
