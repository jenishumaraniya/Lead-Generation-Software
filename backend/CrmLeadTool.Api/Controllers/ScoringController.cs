using CrmLeadTool.Api.Models;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/scoring")]
[Route("api/rules")]
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
    [HttpGet]
    public async Task<IActionResult> GetRules()
    {
        var rules = await _scoringService.GetRulesAsync();
        return Ok(rules);
    }

    [HttpGet("rules/{id}")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRule(int id)
    {
        var rule = await _scoringService.GetRuleByIdAsync(id);
        if (rule == null) return NotFound(new { error = "Rule not found" });
        return Ok(rule);
    }

    [HttpPost("rules")]
    [HttpPost]
    public async Task<IActionResult> CreateRule([FromBody] CreateScoreRuleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.EventType))
        {
            return BadRequest(new { error = "Rule Name and Event Type are required." });
        }

        var normalizedEventType = dto.EventType.Trim().ToUpper();

        // Only allow predefined event types — admins cannot inject arbitrary codes
        if (!ScoringService.PredefinedEventTypes.Contains(normalizedEventType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = $"'{normalizedEventType}' is not a recognized system event type. Allowed values: {string.Join(", ", ScoringService.PredefinedEventTypes)}" });
        }

        // Prevent duplicate event types
        var existing = await _scoringService.GetDistinctEventTypesAsync();
        if (existing.Any(e => string.Equals(e, normalizedEventType, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { error = $"A rule for event type '{normalizedEventType}' already exists. Edit the existing rule instead." });
        }

        var rule = new ScoreRule
        {
            Name = dto.Name.Trim(),
            EventType = normalizedEventType,
            Category = string.IsNullOrWhiteSpace(dto.Category) ? "INTENT" : dto.Category.Trim().ToUpper(),
            Direction = dto.Points >= 0 ? "POSITIVE" : "NEGATIVE",
            Points = dto.Points,
            IsActive = dto.IsActive,
            Description = dto.Description
        };

        var created = await _scoringService.CreateRuleAsync(rule);
        return CreatedAtAction(nameof(GetRule), new { id = created.ScoreRuleId }, created);
    }

    [HttpPut("rules/{id}")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRule(int id, [FromBody] SaveScoreRuleDto dto)
    {
        try
        {
            var updated = new ScoreRule
            {
                Name = dto.Name ?? string.Empty,
                EventType = dto.EventType ?? string.Empty,
                Category = dto.Category ?? "INTENT",
                Direction = dto.Points >= 0 ? "POSITIVE" : "NEGATIVE",
                Points = dto.Points,
                IsActive = dto.IsActive,
                Description = dto.Description
            };

            var rule = await _scoringService.UpdateRuleFullAsync(id, updated);
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

    [HttpPost("rules/{id}/toggle")]
    [HttpPost("{id}/toggle")]
    public async Task<IActionResult> ToggleRule(int id)
    {
        try
        {
            var rule = await _scoringService.ToggleRuleAsync(id);
            return Ok(rule);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("rules/{id}")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRule(int id)
    {
        var deleted = await _scoringService.DeleteRuleAsync(id);
        if (!deleted) return NotFound(new { error = "Rule not found." });
        return Ok(new { message = "Rule deleted successfully." });
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

    [HttpGet("event-types")]
    public async Task<IActionResult> GetEventTypes()
    {
        var types = await _scoringService.GetDistinctEventTypesAsync();
        return Ok(types);
    }

    /// <summary>
    /// Returns the predefined event types that do NOT yet have a ScoreRule
    /// configured. The frontend "Add Rule" modal uses this to populate
    /// its dropdown — ensuring admins only configure system-defined events.
    /// </summary>
    [HttpGet("undefined-event-types")]
    public async Task<IActionResult> GetUndefinedEventTypes()
    {
        var types = await _scoringService.GetUndefinedEventTypesAsync();
        return Ok(types);
    }
}

public class CreateScoreRuleDto
{
    public string Name { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Category { get; set; } = "INTENT";
    public int Points { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class SaveScoreRuleDto
{
    public string? Name { get; set; }
    public string? EventType { get; set; }
    public string? Category { get; set; }
    public int Points { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}

public class UpdateScoreRuleDto
{
    public int Points { get; set; }
    public bool IsActive { get; set; }
}

