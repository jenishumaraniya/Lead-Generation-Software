using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Services;

public class AuditLogService
{
    private readonly AppDbContext _context;

    public AuditLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        int? userId,
        string userEmail,
        string action,
        string entityName,
        string? entityId = null,
        string? details = null,
        string? ipAddress = null)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                UserEmail = string.IsNullOrWhiteSpace(userEmail) ? "SYSTEM" : userEmail,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditLogService] Failed to write audit log: {ex.Message}");
        }
    }

    public async Task<List<AuditLog>> GetLogsAsync(int limit = 100, string? search = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(l =>
                l.UserEmail.ToLower().Contains(s) ||
                l.Action.ToLower().Contains(s) ||
                l.EntityName.ToLower().Contains(s) ||
                (l.Details != null && l.Details.ToLower().Contains(s)));
        }

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
}
