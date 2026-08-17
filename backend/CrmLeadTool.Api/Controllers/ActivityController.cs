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

 

    public ActivityController(AppDbContext context) 

    { 

        _context = context; 

    } 

 

    [HttpPost] 

    public async Task<IActionResult> Create( 

        CreateActivityDto dto) 

    { 

        var visitor = 

            await _context.Visitors 

                .FirstOrDefaultAsync( 

                    x => x.AnonymousId == dto.AnonymousId); 

 

        if (visitor == null) 

        { 

            return NotFound("Visitor not found."); 

        } 

 

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

 

        return Ok(new 

        { 

            message = "Activity recorded", 

            activityId = activity.ActivityId 

        }); 

    } 

}   