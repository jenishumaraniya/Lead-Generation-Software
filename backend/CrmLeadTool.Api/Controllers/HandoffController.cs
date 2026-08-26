using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/handoff")]
public class HandoffController : ControllerBase
{
    private readonly HandoffService _handoffService;

    public HandoffController(HandoffService handoffService)
    {
        _handoffService = handoffService;
    }

    [HttpPost("leads/{leadId}")]
    public async Task<IActionResult> HandoffLead(int leadId, [FromQuery] string destination = "SALES_CRM")
    {
        try
        {
            var handoff = await _handoffService.HandoffLeadAsync(leadId, destination);
            return Ok(new
            {
                success = true,
                handoffId = handoff.LeadHandoffId,
                status = handoff.Status,
                destination = handoff.Destination,
                handedOffAt = handoff.HandedOffAt,
                payload = handoff.PayloadJson
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetHandoffLogs()
    {
        var logs = await _handoffService.GetHandoffLogsAsync();
        return Ok(logs);
    }

    [HttpPost("retry/{handoffId}")]
    public async Task<IActionResult> RetryHandoff(int handoffId)
    {
        try
        {
            var handoff = await _handoffService.RetryHandoffAsync(handoffId);
            return Ok(handoff);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
