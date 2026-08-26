using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/tracking")]
public class TrackingController : ControllerBase
{
    private readonly TrackingService _trackingService;

    public TrackingController(TrackingService trackingService)
    {
        _trackingService = trackingService;
    }

    [HttpGet("open")]
    public async Task<IActionResult> TrackOpen([FromQuery] string tid)
    {
        var userAgent = Request.Headers["User-Agent"].ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _trackingService.TrackOpenAsync(tid, userAgent, ipAddress);

        var pixel = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x21, 0xF9, 0x04, 0x01, 0x00, 0x00, 0x00, 0x00, 0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02, 0x44, 0x01, 0x00, 0x3B };
        return File(pixel, "image/gif");
    }

    [HttpGet("click")]
    public async Task<IActionResult> TrackClick([FromQuery] string tid, [FromQuery] string url)
    {
        var userAgent = Request.Headers["User-Agent"].ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        await _trackingService.TrackClickAsync(tid, url, userAgent, ipAddress);
        return Redirect(url);
    }
}