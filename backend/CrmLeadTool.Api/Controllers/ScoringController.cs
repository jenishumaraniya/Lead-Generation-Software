using CrmLeadTool.Api.Models;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/scoring")]
public class ScoringController : ControllerBase
{
    private readonly ScoringService _scoringService;
    private readonly QualificationService _qualificationService;

    public ScoringController(ScoringService scoringService, QualificationService qualificationService)
    {
        _scoringService = scoringService;
        _qualificationService = qualificationService;
    }

    [HttpGet("rules")]
    public async Task<IActionResult> GetRules()
    {
        var rules = await _scoringService.GetRulesAsync();
        return Ok(rules);
    }

    [HttpPut("rules/{id}")]
    public async Task<IActionResult> UpdateRule(int id, [FromBody] UpdateScoreRuleDto dto)
    {
        try
        {
            var rule = await _scoringService.UpdateRuleAsync(id, dto.Points, dto.IsActive);
            return Ok(rule);
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

    [HttpGet("history/{leadId}")]
    public async Task<IActionResult> GetScoreHistory(int leadId)
    {
        var history = await _scoringService.GetScoreHistoryAsync(leadId);
        return Ok(history);
    }

    [HttpPost("evaluate/{leadId}")]
    public async Task<IActionResult> EvaluateLead(int leadId)
    {
        try
        {
            var result = await _qualificationService.EvaluateQualificationAsync(leadId);
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
}

public class UpdateScoreRuleDto
{
    public int Points { get; set; }
    public bool IsActive { get; set; }
}
