using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/email")]
public class EmailController : ControllerBase
{
    private readonly EmailService _emailService;
    private readonly TrackingService _trackingService;

    public EmailController(EmailService emailService, TrackingService trackingService)
    {
        _emailService = emailService;
        _trackingService = trackingService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail(EmailSendDto dto)
    {
        try
        {
            var result = await _emailService.SendEmailAsync(dto.CampaignRecipientId, dto.FromEmail);
            return Ok(new
            {
                messageId = result.EmailMessageId,
                sentAt = result.SentAt,
                status = result.Status
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Webhook endpoint for Mailtrap (or other provider) events
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] MailtrapWebhookDto dto)
    {
        try
        {
            // Map Mailtrap event to our internal format
            var eventDto = new EmailEventWebhookDto
            {
                EventType = dto.Event.ToUpper(),
                ProviderMessageId = dto.Message.Message_Id,
                RecipientEmail = dto.Recipient,
                ClickUrl = dto.Url,
                UserAgent = dto.User_Agent,
                IpAddress = dto.Ip,
                Timestamp = dto.Timestamp,
                ProviderEventId = $"{dto.Message.Message_Id}_{dto.Event}"
            };

            await _emailService.ProcessEmailEventAsync(eventDto);
            return Ok();
        }
        catch (Exception ex)
        {
            // Log error but return 200 to prevent retries
            return Ok();
        }
    }

    // Tracking pixel endpoint (for opens)
    [HttpGet("tracking/open")]
    public async Task<IActionResult> TrackOpen([FromQuery] string tid)
    {
        var userAgent = Request.Headers["User-Agent"].ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        await _trackingService.TrackOpenAsync(tid, userAgent, ipAddress);

        // Return 1x1 transparent GIF
        var pixel = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x21, 0xF9, 0x04, 0x01, 0x00, 0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02, 0x44, 0x01, 0x00, 0x3B };
        return File(pixel, "image/gif");
    }

    // Click redirect endpoint
    [HttpGet("tracking/click")]
    public async Task<IActionResult> TrackClick([FromQuery] string tid, [FromQuery] string url)
    {
        var userAgent = Request.Headers["User-Agent"].ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        await _trackingService.TrackClickAsync(tid, url, userAgent, ipAddress);
        return Redirect(url);
    }
}