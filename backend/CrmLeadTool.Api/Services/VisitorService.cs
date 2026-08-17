using Microsoft.EntityFrameworkCore;
using CrmLeadTool.Api.Data; 
using CrmLeadTool.Api.Models; 

namespace CrmLeadTool.Api.Services; 
public class VisitorService 

{ 
    private readonly AppDbContext _context; 

 

    public VisitorService(AppDbContext context) 

    { 

        _context = context; 

    } 

 

    public async Task<Visitor> CreateVisitor( 

        string consentStatus = "UNKNOWN") 

    { 

        var anonymousId = 

            $"VIS-{Guid.NewGuid():N}"; 

 

        var visitor = new Visitor 

        { 

            AnonymousId = anonymousId, 

            FirstSeenAt = DateTime.UtcNow, 

            LastSeenAt = DateTime.UtcNow, 

            ConsentStatus = consentStatus, 

            CreatedAt = DateTime.UtcNow 

        }; 

 

        _context.Visitors.Add(visitor); 

 

        await _context.SaveChangesAsync(); 

 

        return visitor; 

    } 

 

    public async Task<Visitor?> GetVisitor( 

        string anonymousId) 

    { 

        return await _context.Visitors 

            .FirstOrDefaultAsync( 

                x => x.AnonymousId == anonymousId); 

    } 

} 