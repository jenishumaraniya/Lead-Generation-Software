using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Services;

public class QualificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<QualificationService> _logger;

    public QualificationService(AppDbContext context, ILogger<QualificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<QualificationResult> EvaluateQualificationAsync(int leadId)
    {
        var lead = await _context.Leads
            .Include(l => l.Visitor)
                .ThenInclude(v => v!.Activities)
            .Include(l => l.Prospect)
                .ThenInclude(p => p!.ProfessionalProfile)
            .Include(l => l.Prospect)
                .ThenInclude(p => p!.Company)
                    .ThenInclude(c => c!.Enrichment)
            .FirstOrDefaultAsync(l => l.LeadId == leadId);

        if (lead == null)
            throw new ArgumentException($"Lead with ID {leadId} not found");

        var score = lead.Score ?? 0;
        var requirement = lead.BusinessRequirement ?? string.Empty;
        var hasGenuineRequirement = !string.IsNullOrWhiteSpace(requirement) && 
                                    requirement.Trim().Length >= 10 && 
                                    !requirement.ToLower().Contains("test") &&
                                    !requirement.ToLower().Contains("spam");

        var jobTitle = (lead.JobTitle ?? lead.Prospect?.JobTitle ?? lead.Prospect?.ProfessionalProfile?.Title ?? "").ToLower();
        var isDecisionMaker = jobTitle.Contains("vp") || 
                              jobTitle.Contains("director") || 
                              jobTitle.Contains("head") || 
                              jobTitle.Contains("chief") || 
                              jobTitle.Contains("manager") ||
                              jobTitle.Contains("lead") ||
                              jobTitle.Contains("founder") ||
                              jobTitle.Contains("owner");

        var hasEmail = !string.IsNullOrWhiteSpace(lead.Email) && lead.Email.Contains("@") && !lead.Email.EndsWith(".invalid");

        string qualificationStage;
        string reason;

        if (!hasEmail || lead.Status == "DISQUALIFIED")
        {
            qualificationStage = "DISQUALIFIED";
            reason = "Invalid contact credentials or explicitly disqualified";
        }
        else if (score >= 60 && hasGenuineRequirement && isDecisionMaker)
        {
            qualificationStage = "SQL"; // Sales Qualified Lead (Hot)
            reason = $"High score ({score} pts), verified Decision Maker ({lead.JobTitle ?? "Target Role"}), and verified business requirement intent.";
        }
        else if (score >= 35 || (score >= 20 && hasGenuineRequirement))
        {
            qualificationStage = "MQL"; // Marketing Qualified Lead
            reason = $"Moderate score ({score} pts) and active inbound/outbound engagement signals.";
        }
        else
        {
            qualificationStage = "COLD";
            reason = $"Early stage prospect/lead with initial score ({score} pts). Needs nurture.";
        }

        lead.Qualification = qualificationStage;
        lead.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new QualificationResult
        {
            LeadId = leadId,
            Stage = qualificationStage,
            Score = score,
            Reason = reason,
            HasGenuineRequirement = hasGenuineRequirement,
            IsDecisionMaker = isDecisionMaker
        };
    }
}

public class QualificationResult
{
    public int LeadId { get; set; }
    public string Stage { get; set; } = "COLD";
    public int Score { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool HasGenuineRequirement { get; set; }
    public bool IsDecisionMaker { get; set; }
}
