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
                status = lead.Status,
                assignedTo = lead.AssignedTo,
                isMultiCategory = lead.IsMultiCategory
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private (string? Role, int? UserId) ExtractUserClaims()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        int? userId = int.TryParse(sub, out int parsed) ? parsed : null;

        if (!string.IsNullOrEmpty(role) && userId.HasValue) return (role, userId);

        var authHeader = Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var tokenString = authHeader.Substring("Bearer ".Length).Trim();
            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(tokenString);
                var tokenRole = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value;
                var tokenSub = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub" || c.Type == "nameid")?.Value;
                int? tokenUserId = int.TryParse(tokenSub, out int p) ? p : null;
                return (tokenRole, tokenUserId);
            }
            catch {}
        }

        return (null, null);
    }

    [HttpGet]
    public async Task<IActionResult> GetLeads([FromQuery] int? assignedTo = null)
    {
        var (role, userId) = ExtractUserClaims();

        int? filterAssignedTo = assignedTo;
        if ((role == "SALES_REP" || role == "SALES") && userId.HasValue)
        {
            filterAssignedTo = userId.Value;
        }

        var leads = await _leadService.GetAllLeadsAsync(filterAssignedTo);
        return Ok(leads);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLead(int id)
    {
        var lead = await _leadService.GetLeadByIdAsync(id);
        if (lead == null)
            return NotFound(new { error = "Lead not found" });

        var (role, userId) = ExtractUserClaims();

        if ((role == "SALES_REP" || role == "SALES") && userId.HasValue)
        {
            dynamic dynLead = lead;
            if (dynLead.AssignedTo != userId.Value)
            {
                return Forbid();
            }
        }

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
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "User";
            var lead = await _leadService.UpdateLeadAsync(id, dto, userEmail);
            return Ok(new
            {
                leadId = lead.LeadId,
                status = lead.Status,
                qualification = lead.Qualification,
                score = lead.Score,
                assignedTo = lead.AssignedTo,
                nextFollowUpDate = lead.NextFollowUpDate,
                notes = lead.Notes
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

    [HttpPost("{id}/assign")]
    public async Task<IActionResult> AssignLead(int id, [FromBody] AssignLeadDto dto)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound(new { error = "Lead not found" });

        int? targetUserId = (dto.EmployeeId > 0) ? dto.EmployeeId : null;
        lead.AssignedTo = targetUserId;

        var user = targetUserId.HasValue ? await _context.Users.Include(u => u.Category).FirstOrDefaultAsync(u => u.UserId == targetUserId.Value) : null;
        var adminEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Admin";

        var act = new LeadActivity
        {
            LeadId = id,
            ActivityType = "ASSIGNMENT",
            Description = user != null 
                ? $"Lead assigned to {user.FullName} (Category: {user.Category?.CategoryName ?? "None"})" 
                : "Lead unassigned by Admin",
            CreatedBy = adminEmail,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeadActivities.Add(act);
        lead.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { 
            message = user != null ? $"Lead assigned to {user.FullName}" : "Lead unassigned", 
            assignedTo = targetUserId,
            salespersonName = user?.FullName
        });
    }

    [HttpPost("{id}/activity")]
    public async Task<IActionResult> AddActivity(int id, [FromBody] LeadActivityInputDto dto)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound(new { error = "Lead not found" });

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Sales Representative";
        var act = new LeadActivity
        {
            LeadId = id,
            ActivityType = string.IsNullOrWhiteSpace(dto.ActivityType) ? "NOTE" : dto.ActivityType.ToUpper(),
            Description = dto.Description ?? "",
            CreatedBy = userEmail,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeadActivities.Add(act);
        lead.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            act.LeadActivityId,
            act.LeadId,
            act.ActivityType,
            act.Description,
            act.CreatedBy,
            ActivityDate = act.CreatedAt
        });
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

    [HttpPost("{id}/notes")]
    [HttpPost("{id}/note")]
    public async Task<IActionResult> AddNote(int id, [FromBody] AddLeadNoteDto dto)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) return NotFound(new { error = "Lead not found" });

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "User";
        var note = new LeadNote
        {
            LeadId = id,
            NoteText = dto.NoteText ?? string.Empty,
            CreatedBy = userEmail,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeadNotes.Add(note);
        lead.Notes = dto.NoteText;
        lead.UpdatedAt = DateTime.UtcNow;

        _context.LeadActivities.Add(new LeadActivity
        {
            LeadId = id,
            ActivityType = "NOTE",
            Description = dto.NoteText,
            CreatedBy = userEmail,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            note.LeadNoteId,
            note.LeadId,
            note.NoteText,
            note.CreatedBy,
            note.CreatedAt
        });
    }

    [HttpGet("{id}/notes")]
    public async Task<IActionResult> GetNotes(int id)
    {
        var notes = await _context.LeadNotes
            .Where(n => n.LeadId == id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new
            {
                n.LeadNoteId,
                n.LeadId,
                n.NoteText,
                n.CreatedBy,
                n.CreatedAt
            })
            .ToListAsync();

        return Ok(notes);
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