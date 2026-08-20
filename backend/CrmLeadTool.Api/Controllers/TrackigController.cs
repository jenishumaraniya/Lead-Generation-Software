using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/tracking")]
public class TrackingController : ControllerBase
{
    private readonly AppDbContext _context;

    public TrackingController(AppDbContext context)
    {
        _context = context;
    }

    // Tracking pixel for opens
    [HttpGet("open")]
    public async Task<IActionResult> TrackOpen([FromQuery] string tid)
    {
        if (string.IsNullOrEmpty(tid))
            return BadRequest();

        var parts = tid.Split('_');
        if (parts.Length < 2 || !int.TryParse(parts[0], out int recipientId))
            return BadRequest();

        // Find the most recent email for this recipient
        var emailMessage = await _context.EmailMessages
            .Where(em => em.CampaignRecipientId == recipientId)
            .OrderByDescending(em => em.SentAt)
            .FirstOrDefaultAsync();

        if (emailMessage != null)
        {
            // Record open event
            var emailEvent = new EmailEvent
            {
                EmailMessageId = emailMessage.EmailMessageId,
                EventType = "OPEN",
                EventTimestamp = DateTime.UtcNow,
                UserAgent = Request.Headers["User-Agent"].ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _context.EmailEvents.Add(emailEvent);
            
            // Update EmailMessage status
            emailMessage.OpenedAt = DateTime.UtcNow;
            emailMessage.Status = "OPENED";
            
            await _context.SaveChangesAsync();
        }

        // Return a transparent 1x1 GIF pixel
        var pixel = new byte[] { 
            0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 
            0x80, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x21, 
            0xF9, 0x04, 0x01, 0x00, 0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 
            0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02, 0x44, 
            0x01, 0x00, 0x3B 
        };
        return File(pixel, "image/gif");
    }

    // Click redirect
    [HttpGet("click")]
    public async Task<IActionResult> TrackClick([FromQuery] string tid, [FromQuery] string url)
    {
        if (string.IsNullOrEmpty(tid) || string.IsNullOrEmpty(url))
            return BadRequest();

        var parts = tid.Split('_');
        if (parts.Length < 2 || !int.TryParse(parts[0], out int recipientId))
            return BadRequest();

        // Find the most recent email for this recipient
        var emailMessage = await _context.EmailMessages
            .Where(em => em.CampaignRecipientId == recipientId)
            .OrderByDescending(em => em.SentAt)
            .FirstOrDefaultAsync();

        if (emailMessage != null)
        {
            // Record click event
            var emailEvent = new EmailEvent
            {
                EmailMessageId = emailMessage.EmailMessageId,
                EventType = "CLICK",
                EventTimestamp = DateTime.UtcNow,
                UserAgent = Request.Headers["User-Agent"].ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                ClickUrl = url
            };
            _context.EmailEvents.Add(emailEvent);
            
            // Update EmailMessage status
            emailMessage.ClickedAt = DateTime.UtcNow;
            emailMessage.Status = "CLICKED";
            
            await _context.SaveChangesAsync();
        }

        // Redirect to the original URL
        return Redirect(url);
    }
}