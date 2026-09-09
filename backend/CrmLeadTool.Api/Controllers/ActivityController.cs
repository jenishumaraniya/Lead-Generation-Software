using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/activity")]
public class ActivityController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly Services.ScoringService _scoringService;
    private readonly Services.QualificationService _qualificationService;

    public ActivityController(
        AppDbContext context, 
        Services.ScoringService scoringService, 
        Services.QualificationService qualificationService)
    {
        _context = context;
        _scoringService = scoringService;
        _qualificationService = qualificationService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateActivityDto dto)
    {
        var visitor = await _context.Visitors
            .FirstOrDefaultAsync(v => v.AnonymousId == dto.AnonymousId);

        if (visitor == null)
            return NotFound("Visitor not found.");

        visitor.LastSeenAt = DateTime.UtcNow;

        var activity = new VisitorActivity
        {
            VisitorId = visitor.VisitorId,
            ActivityType = dto.ActivityType,
            ProductId = dto.ProductId,
            PageUrl = dto.PageUrl,
            Metadata = dto.Metadata,
            Timestamp = DateTime.UtcNow
        };

        _context.VisitorActivities.Add(activity);
        await _context.SaveChangesAsync();

        // If the visitor already converted to a lead, score INTEREST_CLICK in real time
        // Consecutive tapping on the same product does not add points repeatedly;
        // Enforce strict 2-minute cooldown (1 count per product every 2 minutes).
        if (dto.ActivityType == "INTEREST_CLICK")
        {
            var existingLead = await _context.Leads
                .Where(l => l.VisitorId == visitor.VisitorId)
                .OrderByDescending(l => l.CreatedAt)
                .FirstOrDefaultAsync();

            if (existingLead != null)
            {
                var prod = dto.ProductId.HasValue ? await _context.Products.FindAsync(dto.ProductId.Value) : null;
                var prodName = prod?.Name ?? "Product";
                var twoMinutesAgo = DateTime.UtcNow.AddMinutes(-2);

                string reasonSearch = $"Interest expressed in product: {prodName}";
                string prodTag = dto.ProductId.HasValue ? $"[ProdId:{dto.ProductId.Value}]" : "";

                bool scoredRecently = await _context.LeadScoreHistories.AnyAsync(h =>
                    h.LeadId == existingLead.LeadId &&
                    h.EventType == "INTEREST_CLICK" &&
                    (h.Reason.Contains(reasonSearch) || (!string.IsNullOrEmpty(prodTag) && h.Reason.Contains(prodTag))) &&
                    h.Timestamp >= twoMinutesAgo
                );

                if (!scoredRecently)
                {
                    string fullReason = string.IsNullOrEmpty(prodTag) 
                        ? reasonSearch 
                        : $"{reasonSearch} {prodTag}";

                    await _scoringService.ApplyScoreEventAsync(
                        existingLead.LeadId, 
                        "INTEREST_CLICK", 
                        fullReason, 
                        allowDuplicates: true
                    );
                    await _qualificationService.EvaluateQualificationAsync(existingLead.LeadId);
                }
            }
        }

        return Ok(new
        {
            message = "Activity recorded",
            activityId = activity.ActivityId
        });
    }
}