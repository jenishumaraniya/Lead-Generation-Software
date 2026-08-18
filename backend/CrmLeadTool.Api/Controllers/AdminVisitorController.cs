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

    [HttpGet]
    public async Task<IActionResult> GetVisitors()
    {
        var visitors = await _context.Visitors
            .Select(v => new
            {
                v.VisitorId,
                v.AnonymousId,
                v.FirstSeenAt,
                v.LastSeenAt,
                Activities = v.Activities.Count,
                LastActivity = v.Activities
                    .OrderByDescending(a => a.Timestamp)
                    .Select(a => a.ActivityType)
                    .FirstOrDefault()
            })
            .OrderByDescending(x => x.LastSeenAt)
            .ToListAsync();

        return Ok(visitors);
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

        return Ok(visitor);
    }
}