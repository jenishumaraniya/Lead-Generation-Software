using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/lead")]
public class LeadController : ControllerBase
{
    private readonly LeadService _leadService;
    private readonly QualificationService _qualificationService;
    private readonly ScoringService _scoringService;

    public LeadController(
        LeadService leadService,
        QualificationService qualificationService,
        ScoringService scoringService)
    {
        _leadService = leadService;
        _qualificationService = qualificationService;
        _scoringService = scoringService;
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
                message = "Lead processed successfully.",
                qualification = lead.Qualification,
                score = lead.Score,
                status = lead.Status
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetLeads()
    {
        var leads = await _leadService.GetAllLeadsAsync();
        return Ok(leads);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLead(int id)
    {
        var lead = await _leadService.GetLeadByIdAsync(id);
        if (lead == null)
            return NotFound();
        return Ok(lead);
    }

    [HttpPost("{id}/qualify")]
    public async Task<IActionResult> QualifyLead(int id)
    {
        try
        {
            var result = await _qualificationService.EvaluateQualificationAsync(id);
            return Ok(result);
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

    [HttpGet("{id}/score-history")]
    public async Task<IActionResult> GetScoreHistory(int id)
    {
        var history = await _scoringService.GetScoreHistoryAsync(id);
        return Ok(history);
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
                score = lead.Score
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}