using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Services;

public class ScoringService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ScoringService> _logger;

    public ScoringService(AppDbContext context, ILogger<ScoringService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task EnsureDefaultRulesAsync()
    {
        if (await _context.ScoreRules.AnyAsync()) return;

        var defaultRules = new List<ScoreRule>
        {
            new() { Name = "Form Submission Inquiry", EventType = "FORM_SUBMIT", Category = "INTENT", Direction = "POSITIVE", Points = 25, IsActive = true, Description = "Visitor submitted a quote or demo request" },
            new() { Name = "Product View", EventType = "PRODUCT_VIEW", Category = "ENGAGEMENT", Direction = "POSITIVE", Points = 5, IsActive = true, Description = "Visitor viewed a product details page" },
            new() { Name = "Repeat Website Visit", EventType = "REPEAT_VISIT", Category = "ENGAGEMENT", Direction = "POSITIVE", Points = 10, IsActive = true, Description = "Visitor returned for multiple sessions" },
            new() { Name = "Target Role Match (Decision Maker)", EventType = "ROLE_MATCH", Category = "FIT", Direction = "POSITIVE", Points = 20, IsActive = true, Description = "Job title matches VP, Director, Head, or C-level" },
            new() { Name = "Target Company Fit", EventType = "COMPANY_FIT", Category = "FIT", Direction = "POSITIVE", Points = 15, IsActive = true, Description = "Company size, industry, or revenue in target ICP" },
            new() { Name = "LinkedIn Profile Enriched", EventType = "LINKEDIN_ENRICHED", Category = "ENRICHMENT", Direction = "POSITIVE", Points = 10, IsActive = true, Description = "Successfully enriched with verified LinkedIn data" },
            new() { Name = "Email Opened", EventType = "EMAIL_OPEN", Category = "ENGAGEMENT", Direction = "POSITIVE", Points = 5, IsActive = true, Description = "Recipient opened an outbound sequence email" },
            new() { Name = "Email Link Clicked", EventType = "EMAIL_CLICK", Category = "ENGAGEMENT", Direction = "POSITIVE", Points = 15, IsActive = true, Description = "Recipient clicked a link inside email" },
            new() { Name = "Email Replied", EventType = "EMAIL_REPLY", Category = "ENGAGEMENT", Direction = "POSITIVE", Points = 30, IsActive = true, Description = "Recipient responded to outbound email" },
            new() { Name = "Email Hard Bounce", EventType = "EMAIL_BOUNCE", Category = "COMPLIANCE", Direction = "NEGATIVE", Points = -30, IsActive = true, Description = "Message bounced permanently" },
            new() { Name = "Irrelevant Requirement Intent", EventType = "IRRELEVANT_REQ", Category = "INTENT", Direction = "NEGATIVE", Points = -20, IsActive = true, Description = "Business requirement is personal or non-commercial" },
            new() { Name = "Invalid / Incomplete Contact", EventType = "INVALID_CONTACT", Category = "FIT", Direction = "NEGATIVE", Points = -25, IsActive = true, Description = "Invalid email domain or incomplete contact credentials" }
        };

        _context.ScoreRules.AddRange(defaultRules);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ScoreRule>> GetRulesAsync()
    {
        await EnsureDefaultRulesAsync();
        return await _context.ScoreRules.OrderBy(r => r.Category).ThenByDescending(r => r.Points).ToListAsync();
    }

    public async Task<ScoreRule> UpdateRuleAsync(int ruleId, int points, bool isActive)
    {
        var rule = await _context.ScoreRules.FindAsync(ruleId);
        if (rule == null) throw new ArgumentException($"Score rule {ruleId} not found");

        rule.Points = points;
        rule.IsActive = isActive;
        rule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task<LeadScoreHistory?> ApplyScoreEventAsync(int leadId, string eventType, string? customReason = null, int? customDelta = null)
    {
        await EnsureDefaultRulesAsync();

        var lead = await _context.Leads.FindAsync(leadId);
        if (lead == null) return null;

        var rule = await _context.ScoreRules.FirstOrDefaultAsync(r => r.EventType == eventType && r.IsActive);
        int delta = customDelta ?? (rule?.Points ?? 0);
        string ruleName = rule?.Name ?? eventType;

        int currentScore = lead.Score ?? 0;
        int newScore = Math.Max(0, currentScore + delta);
        lead.Score = newScore;
        lead.UpdatedAt = DateTime.UtcNow;

        var history = new LeadScoreHistory
        {
            LeadId = leadId,
            RuleId = rule?.ScoreRuleId,
            RuleName = ruleName,
            EventType = eventType,
            Delta = delta,
            TotalScore = newScore,
            Reason = customReason ?? (rule?.Description ?? $"Applied {eventType}"),
            Timestamp = DateTime.UtcNow
        };

        _context.LeadScoreHistories.Add(history);
        await _context.SaveChangesAsync();

        return history;
    }

    public async Task<List<LeadScoreHistory>> GetScoreHistoryAsync(int leadId)
    {
        return await _context.LeadScoreHistories
            .Where(h => h.LeadId == leadId)
            .OrderByDescending(h => h.Timestamp)
            .ToListAsync();
    }
}
