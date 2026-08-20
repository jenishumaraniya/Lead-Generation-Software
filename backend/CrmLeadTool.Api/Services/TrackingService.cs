using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Services;

public class TrackingService
{
    private readonly AppDbContext _context;

    public TrackingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task TrackOpenAsync(string trackingId, string userAgent, string ipAddress)
    {
        await TrackEventAsync(trackingId, "OPEN", userAgent, ipAddress, null);
    }

    public async Task<string> TrackClickAsync(string trackingId, string url, string userAgent, string ipAddress)
    {
        await TrackEventAsync(trackingId, "CLICK", userAgent, ipAddress, url);
        return url; // return the original URL for redirect
    }

    private async Task TrackEventAsync(string trackingId, string eventType, string userAgent, string ipAddress, string? url)
    {
        var parts = trackingId.Split('_');
        if (parts.Length < 2 || !int.TryParse(parts[0], out int recipientId))
            return;

        // Find the most recent email for this recipient
        var emailMessage = await _context.EmailMessages
            .Where(em => em.CampaignRecipientId == recipientId)
            .OrderByDescending(em => em.SentAt)
            .FirstOrDefaultAsync();

        if (emailMessage == null)
            return;

        var emailEvent = new EmailEvent
        {
            EmailMessageId = emailMessage.EmailMessageId,
            EventType = eventType,
            EventTimestamp = DateTime.UtcNow,
            UserAgent = userAgent,
            IpAddress = ipAddress,
            ClickUrl = url
        };
        _context.EmailEvents.Add(emailEvent);

        // Update EmailMessage status
        if (eventType == "OPEN")
            emailMessage.OpenedAt = emailEvent.EventTimestamp;
        else if (eventType == "CLICK")
            emailMessage.ClickedAt = emailEvent.EventTimestamp;

        await _context.SaveChangesAsync();
    }
}