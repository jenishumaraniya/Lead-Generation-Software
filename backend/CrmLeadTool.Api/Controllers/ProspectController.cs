using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/prospects")]
public class ProspectController : ControllerBase
{
    private readonly ProspectService _prospectService;

    public ProspectController(ProspectService prospectService)
    {
        _prospectService = prospectService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProspect(CreateProspectDto dto)
    {
        try
        {
            var prospect = await _prospectService.CreateProspectAsync(dto);
            return CreatedAtAction(nameof(GetProspect), new { id = prospect.ProspectId }, prospect);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
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
            return NotFound();
        return Ok(prospect);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProspect(int id, UpdateProspectDto dto)
    {
        try
        {
            var prospect = await _prospectService.UpdateProspectAsync(id, dto);
            return Ok(prospect);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}