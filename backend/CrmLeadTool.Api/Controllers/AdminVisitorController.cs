using CrmLeadTool.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/admin/visitors")]
public class AdminVisitorController : ControllerBase
{
    private readonly AppDbContext _context;

    public AdminVisitorController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetVisitorStats()
    {
        var totalVisitors = await _context.Visitors.CountAsync();
        var today = DateTime.UtcNow.Date;
        var activeToday = await _context.Visitors.CountAsync(v => v.LastSeenAt >= today);
        var totalActivities = await _context.VisitorActivities.CountAsync();
        var interestClicks = await _context.VisitorActivities.CountAsync(a => a.ActivityType == "INTEREST_CLICK");
        
        var convertedCount = await _context.Leads
            .Where(l => l.VisitorId.HasValue)
            .Select(l => l.VisitorId!.Value)
            .Distinct()
            .CountAsync();

        var conversionRate = totalVisitors > 0 
            ? Math.Round((double)convertedCount / totalVisitors * 100, 1) 
            : 0;

        return Ok(new
        {
            totalVisitors,
            activeToday,
            totalActivities,
            interestClicks,
            convertedCount,
            conversionRate
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetVisitors()
    {
        var visitors = await _context.Visitors
            .OrderByDescending(v => v.LastSeenAt)
            .Take(150)
            .Select(v => new
            {
                v.VisitorId,
                v.AnonymousId,
                v.PublicId,
                v.ConsentStatus,
                v.FirstSeenAt,
                v.LastSeenAt,
                TotalActivities = v.Activities.Count,
                InterestClicksCount = v.Activities.Count(a => a.ActivityType == "INTEREST_CLICK"),
                ProductViewsCount = v.Activities.Count(a => a.ActivityType == "PRODUCT_VIEW"),
                LastActivity = v.Activities
                    .OrderByDescending(a => a.Timestamp)
                    .Select(a => new
                    {
                        a.ActivityType,
                        a.PageUrl,
                        a.Timestamp,
                        ProductName = a.Product != null ? a.Product.Name : null
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var visitorIds = visitors.Select(v => v.VisitorId).ToList();
        var leads = await _context.Leads
            .Where(l => l.VisitorId.HasValue && visitorIds.Contains(l.VisitorId.Value))
            .Select(l => new
            {
                l.VisitorId,
                l.LeadId,
                l.FullName,
                l.Email,
                l.CompanyName,
                l.JobTitle,
                l.Score,
                l.Status,
                l.Qualification,
                l.CreatedAt
            })
            .ToListAsync();

        var leadMap = leads
            .GroupBy(l => l.VisitorId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

        var result = visitors.Select(v =>
        {
            leadMap.TryGetValue(v.VisitorId, out var lead);
            return new
            {
                v.VisitorId,
                v.AnonymousId,
                v.PublicId,
                v.ConsentStatus,
                v.FirstSeenAt,
                v.LastSeenAt,
                v.TotalActivities,
                v.InterestClicksCount,
                v.ProductViewsCount,
                LastActivityType = v.LastActivity?.ActivityType,
                LastActivityTime = v.LastActivity?.Timestamp,
                LastPageUrl = v.LastActivity?.PageUrl,
                LastProductName = v.LastActivity?.ProductName,
                IsConverted = lead != null,
                Lead = lead != null ? new
                {
                    lead.LeadId,
                    lead.FullName,
                    lead.Email,
                    lead.CompanyName,
                    lead.JobTitle,
                    lead.Score,
                    lead.Status,
                    lead.Qualification
                } : null
            };
        });

        return Ok(result);
    }

    [HttpGet("{anonymousId}")]
    public async Task<IActionResult> GetVisitorDetails(string anonymousId)
    {
        var visitor = await _context.Visitors
            .Include(v => v.Activities)
                .ThenInclude(a => a.Product)
            .FirstOrDefaultAsync(v => v.AnonymousId == anonymousId);

        if (visitor == null)
            return NotFound();

        var lead = await _context.Leads
            .Where(l => l.VisitorId == visitor.VisitorId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                l.LeadId,
                l.FullName,
                l.Email,
                l.CompanyName,
                l.JobTitle,
                l.Score,
                l.Status,
                l.Qualification,
                l.CreatedAt
            })
            .FirstOrDefaultAsync();

        var activities = visitor.Activities
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new
            {
                a.ActivityId,
                a.ActivityType,
                a.PageUrl,
                a.Timestamp,
                a.Metadata,
                ProductId = a.ProductId,
                ProductName = a.Product != null ? a.Product.Name : null,
                ProductPrice = a.Product != null ? a.Product.Pricing : (decimal?)null
            })
            .ToList();

        return Ok(new
        {
            visitor.VisitorId,
            visitor.AnonymousId,
            visitor.PublicId,
            visitor.ConsentStatus,
            visitor.FirstSeenAt,
            visitor.LastSeenAt,
            IsConverted = lead != null,
            Lead = lead,
            Activities = activities
        });
    }
}