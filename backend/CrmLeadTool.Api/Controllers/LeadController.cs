using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/lead")]
[Route("api/leads")]
public class LeadController : ControllerBase
{
    private readonly LeadService _leadService;
    private readonly QualificationService _qualificationService;
    private readonly ScoringService _scoringService;
    private readonly AppDbContext _context;

    public LeadController(
        LeadService leadService,
        QualificationService qualificationService,
        ScoringService scoringService,
        AppDbContext context)
    {
        _leadService = leadService;
        _qualificationService = qualificationService;
        _scoringService = scoringService;
        _context = context;
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

    [HttpPost("{id}/activity")]
    public async Task<IActionResult> AddActivity(int id, [FromBody] LeadActivityInputDto dto)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound(new { error = "Lead not found" });

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "System";
        var act = new LeadActivity
        {
            LeadId = id,
            ActivityType = dto.ActivityType ?? "NOTE",
            Description = dto.Description ?? "",
            CreatedBy = userEmail,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeadActivities.Add(act);
        lead.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(act);
    }

    [HttpGet("{id}/activities")]
    public async Task<IActionResult> GetActivities(int id)
    {
        var activities = await _context.LeadActivities
            .Where(a => a.LeadId == id)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.LeadActivityId,
                a.LeadId,
                a.ActivityType,
                a.Description,
                a.CreatedBy,
                ActivityDate = a.CreatedAt
            })
            .ToListAsync();

        return Ok(activities);
    }

    [HttpPost("{id}/assign")]
    public async Task<IActionResult> AssignLead(int id, [FromBody] AssignLeadDto dto)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound(new { error = "Lead not found" });

        var user = await _context.Users.FindAsync(dto.EmployeeId);
        var act = new LeadActivity
        {
            LeadId = id,
            ActivityType = "ASSIGNMENT",
            Description = $"Lead assigned to {(user != null ? user.FullName : $"Employee #{dto.EmployeeId}")}",
            CreatedBy = User.FindFirstValue(ClaimTypes.Email) ?? "Admin",
            CreatedAt = DateTime.UtcNow
        };

        _context.LeadActivities.Add(act);
        lead.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Lead assigned successfully", employeeId = dto.EmployeeId });
    }

    [HttpPost("{id}/note")]
    public async Task<IActionResult> AddNote(int id, [FromBody] string note)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound(new { error = "Lead not found" });

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "User";
        var act = new LeadActivity
        {
            LeadId = id,
            ActivityType = "NOTE",
            Description = note,
            CreatedBy = userEmail,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeadActivities.Add(act);
        lead.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Note added" });
    }
}

public class LeadActivityInputDto
{
    public string? ActivityType { get; set; }
    public string? Description { get; set; }
}

public class AssignLeadDto
{
    public int EmployeeId { get; set; }
}