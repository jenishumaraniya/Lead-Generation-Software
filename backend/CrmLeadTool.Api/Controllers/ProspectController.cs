using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/prospects")]
public class ProspectController : ControllerBase
{
    private readonly ProspectService _prospectService;
    private readonly ProspectDiscoveryService _discoveryService;
    private readonly LinkedInEnrichmentService _enrichmentService;

    public ProspectController(
        ProspectService prospectService,
        ProspectDiscoveryService discoveryService,
        LinkedInEnrichmentService enrichmentService)
    {
        _prospectService = prospectService;
        _discoveryService = discoveryService;
        _enrichmentService = enrichmentService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProspect([FromBody] CreateProspectDto dto)
    {
        try
        {
            var prospect = await _prospectService.CreateProspectAsync(dto);
            return CreatedAtAction(nameof(GetProspect), new { id = prospect.ProspectId }, prospect);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("discover")]
    public async Task<IActionResult> DiscoverProspects([FromBody] DiscoveryCriteria criteria)
    {
        try
        {
            var result = await _discoveryService.DiscoverProspectsAsync(criteria);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetProspects()
    {
        var prospects = await _prospectService.GetAllProspectsAsync();
        return Ok(prospects);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProspect(int id)
    {
        var prospect = await _prospectService.GetProspectAsync(id);
        if (prospect == null)
            return NotFound(new { error = $"Prospect with ID {id} not found." });
        return Ok(prospect);
    }

    [HttpPost("{id}/enrichment")]
    public async Task<IActionResult> TriggerEnrichment(int id)
    {
        try
        {
            var run = await _enrichmentService.StartEnrichmentAsync(id);
            return Ok(new
            {
                success = true,
                enrichmentRunId = run.EnrichmentRunId,
                status = run.Status,
                startedAt = run.StartedAt,
                completedAt = run.CompletedAt
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

    [HttpGet("{id}/enrichment")]
    public async Task<IActionResult> GetEnrichment(int id)
    {
        var details = await _enrichmentService.GetProspectEnrichmentDetailsAsync(id);
        if (details == null)
            return NotFound(new { error = $"Prospect with ID {id} not found." });
        return Ok(details);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProspect(int id, [FromBody] UpdateProspectDto dto)
    {
        try
        {
            var prospect = await _prospectService.UpdateProspectAsync(id, dto);
            return Ok(prospect);
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProspect(int id)
    {
        try
        {
            await _prospectService.DeleteProspectAsync(id);
            return Ok(new { message = "Prospect deleted successfully." });
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