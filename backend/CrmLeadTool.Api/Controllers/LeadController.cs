using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/lead")]
public class LeadController : ControllerBase
{
    private readonly LeadService _leadService;

    public LeadController(LeadService leadService)
    {
        _leadService = leadService;
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitLead([FromBody] LeadSubmitDto dto)
    {
        try
        {
            var lead = await _leadService.CreateLeadFromFormAsync(dto);
            return Ok(new
            {
                leadId = lead.LeadId,
                message = "Lead created successfully.",
                isDuplicate = false
            });
        }
        catch (DuplicateLeadException ex)
        {
            return Conflict(new
            {
                message = ex.Message,
                duplicateLeadIds = ex.DuplicateLeadIds,
                isDuplicate = true
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetLeads()
    {
        try
        {
            var leads = await _leadService.GetAllLeadsAsync();
            return Ok(leads);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLead(int id)
    {
        var lead = await _leadService.GetLeadByIdAsync(id);
        if (lead == null)
            return NotFound(new { message = "Lead not found." });
        return Ok(lead);
    }

    [HttpPost("search")]
    public async Task<IActionResult> SearchLeads([FromBody] LeadSearchDto dto)
    {
        var leads = await _leadService.SearchLeadsAsync(dto);
        return Ok(leads);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLead(int id, [FromBody] LeadUpdateDto dto)
    {
        try
        {
            var lead = await _leadService.UpdateLeadAsync(id, dto);
            return Ok(new
            {
                leadId = lead.LeadId,
                status = lead.Status,
                qualification = lead.Qualification,
                score = lead.Score,
                message = "Lead updated successfully."
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/note")]
    public async Task<IActionResult> AddNote(int id, [FromBody] string note)
    {
        try
        {
            await _leadService.AddLeadNoteAsync(id, note);
            return Ok(new { message = "Note added successfully." });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("convert-prospect/{prospectId}")]
    public async Task<IActionResult> ConvertProspect(int prospectId, [FromBody] LeadSubmitDto? dto)
    {
        try
        {
            dto ??= new LeadSubmitDto();
            var lead = await _leadService.ConvertProspectToLeadAsync(prospectId, dto);
            return Ok(new
            {
                leadId = lead.LeadId,
                message = "Prospect converted to lead successfully."
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}