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
        var existingEventTypes = await _context.ScoreRules
            .Select(r => r.EventType.ToUpper())
            .ToListAsync();
        var existingSet = new HashSet<string>(existingEventTypes, StringComparer.OrdinalIgnoreCase);

        var defaultRules = new List<ScoreRule>
        {
            new() { Name = "Form Submission Inquiry", EventType = "FORM_SUBMIT", Category = "INTENT", Direction = "POSITIVE", Points = 25, IsActive = true, Description = "Visitor submitted a quote or demo request" },
            new() { Name = "Product Interest Expressed", EventType = "INTEREST_CLICK", Category = "INTENT", Direction = "POSITIVE", Points = 15, IsActive = true, Description = "Prospect expressed interest in a specific product or solution" },
            new() { Name = "Product View", EventType = "PRODUCT_VIEW", Category = "ENGAGEMENT", Direction = "POSITIVE", Points = 5, IsActive = true, Description = "Visitor viewed a product details page" },
            new() { Name = "Repeat Website Visit", EventType = "REPEAT_VISIT", Category = "ENGAGEMENT", Direction = "POSITIVE", Points = 10, IsActive = true, Description = "Visitor returned for multiple sessions" },
            new() { Name = "Target Role Match (Decision Maker)", EventType = "ROLE_MATCH", Category = "FIT", Direction = "POSITIVE", Points = 20, IsActive = true, Description = "Job title matches VP, Director, Head, or C-level" },
            new() { Name = "Target Company Fit", EventType = "COMPANY_FIT", Category = "FIT", Direction = "POSITIVE", Points = 15, IsActive = true, Description = "Company size, industry, or revenue in target ICP" },
            new() { Name = "LinkedIn Profile Enriched", EventType = "LINKEDIN_ENRICHED", Category = "ENRICHMENT", Direction = "POSITIVE", Points = 10, IsActive = true, Description = "Successfully enriched with verified LinkedIn data" },
            new() { Name = "AI Analysis Positive Intent", EventType = "AI_ANALYSIS", Category = "INTENT", Direction = "POSITIVE", Points = 15, IsActive = true, Description = "AI analysis confirmed commercial buying intent or high priority" },
            new() { Name = "AI Analysis Negative / Low Fit", EventType = "AI_ANALYSIS_NEGATIVE", Category = "INTENT", Direction = "NEGATIVE", Points = -15, IsActive = true, Description = "AI analysis flagged low buying intent, competitor, or poor fit" },
            new() { Name = "Email Opened", EventType = "EMAIL_OPEN", Category = "ENGAGEMENT", Direction = "POSITIVE", Points = 5, IsActive = true, Description = "Recipient opened an outbound sequence email" },
            new() { Name = "Email Link Clicked", EventType = "EMAIL_CLICK", Category = "ENGAGEMENT", Direction = "POSITIVE", Points = 15, IsActive = true, Description = "Recipient clicked a link inside email" },
            new() { Name = "Email Replied", EventType = "EMAIL_REPLY", Category = "ENGAGEMENT", Direction = "POSITIVE", Points = 30, IsActive = true, Description = "Recipient responded to outbound email" },
            new() { Name = "Email Hard Bounce", EventType = "EMAIL_BOUNCE", Category = "COMPLIANCE", Direction = "NEGATIVE", Points = -30, IsActive = true, Description = "Message bounced permanently" },
            new() { Name = "Irrelevant Requirement Intent", EventType = "IRRELEVANT_REQ", Category = "INTENT", Direction = "NEGATIVE", Points = -20, IsActive = true, Description = "Business requirement is personal or non-commercial" },
            new() { Name = "Invalid / Incomplete Contact", EventType = "INVALID_CONTACT", Category = "FIT", Direction = "NEGATIVE", Points = -25, IsActive = true, Description = "Invalid email domain or incomplete contact credentials" }
        };

        var missingRules = defaultRules.Where(r => !existingSet.Contains(r.EventType)).ToList();
        if (missingRules.Any())
        {
            _context.ScoreRules.AddRange(missingRules);
            await _context.SaveChangesAsync();
        }

        // Ensure all negative rules have negative points and are active
        var allRules = await _context.ScoreRules.ToListAsync();
        bool anyChanged = false;
        foreach (var r in allRules)
        {
            if (r.Direction == "NEGATIVE" && r.Points > 0)
            {
                r.Points = -r.Points;
                anyChanged = true;
            }
            if (r.EventType == "IRRELEVANT_REQ" && r.Points >= 0) { r.Points = -20; r.Direction = "NEGATIVE"; anyChanged = true; }
            if (r.EventType == "INVALID_CONTACT" && r.Points >= 0) { r.Points = -25; r.Direction = "NEGATIVE"; anyChanged = true; }
            if (r.EventType == "AI_ANALYSIS_NEGATIVE" && r.Points >= 0) { r.Points = -15; r.Direction = "NEGATIVE"; anyChanged = true; }
            if (r.EventType == "EMAIL_BOUNCE" && r.Points >= 0) { r.Points = -30; r.Direction = "NEGATIVE"; anyChanged = true; }
        }
        if (anyChanged)
        {
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<string>> EvaluateNegativeRulesAsync(int leadId)
    {
        await EnsureDefaultRulesAsync();
        var lead = await _context.Leads.FindAsync(leadId);
        if (lead == null) return new List<string>();

        var appliedRules = new List<string>();

        // 1. Evaluate INVALID_CONTACT (-25 pts)
        var contactRedFlags = new List<string>();

        // A) Fake / repeating phone number
        var rawPhone = lead.Phone ?? "";
        var digits = System.Text.RegularExpressions.Regex.Replace(rawPhone, @"\D", "");
        if (digits.StartsWith("91") && digits.Length > 10)
        {
            digits = digits.Substring(digits.Length - 10);
        }

        bool isAllSameDigit = digits.Length >= 6 && System.Text.RegularExpressions.Regex.IsMatch(digits, @"^(\d)\1+$");
        bool hasSixConsecutiveIdentical = System.Text.RegularExpressions.Regex.IsMatch(digits, @"(\d)\1{5,}");
        bool isSequential = digits.Contains("1234567") || digits.Contains("9876543") || digits.Contains("0123456");
        bool isTooShort = digits.Length > 0 && digits.Length < 7;

        if (isAllSameDigit || hasSixConsecutiveIdentical || isSequential || isTooShort)
        {
            contactRedFlags.Add($"Fake/repeating phone number ({rawPhone})");
        }

        // B) Bogus / nonsensical job title / role
        var title = (lead.JobTitle ?? "").Trim();
        var titleLower = title.ToLowerInvariant();
        bool isRepeatedChars = title.Length >= 2 && System.Text.RegularExpressions.Regex.IsMatch(title, @"^([a-zA-Z])\1+$");
        var knownBogus = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ccc", "aaa", "bbb", "xxx", "zzz", "yyy", "asdf", "qwer", "test", "testing",
            "na", "n/a", "none", "null", "qwerty", "fake", "temp", "xyz", "abc", "student", "dummy"
        };
        bool isKnownBogusTitle = knownBogus.Contains(titleLower);
        bool isGibberishShort = title.Length > 0 && title.Length <= 3 && 
            !new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
            { "ceo", "cto", "cfo", "coo", "cio", "cmo", "cro", "vp", "md", "hr", "pm", "se", "qa", "gm", "dev" }.Contains(titleLower);

        if (isRepeatedChars || isKnownBogusTitle || isGibberishShort)
        {
            contactRedFlags.Add($"Bogus/nonsensical job title ('{title}')");
        }

        // C) Disposable or invalid email
        var email = (lead.Email ?? "").Trim().ToLowerInvariant();
        bool isDisposable = email.Contains("tempmail") || email.Contains("10minutemail") || email.Contains("mailinator") || email.Contains("trashmail");
        if (isDisposable || (!string.IsNullOrEmpty(email) && !email.Contains("@")))
        {
            contactRedFlags.Add($"Disposable or malformed email ({email})");
        }

        if (contactRedFlags.Any())
        {
            var reason = $"Invalid or suspicious contact credentials: {string.Join("; ", contactRedFlags)}";
            var result = await ApplyScoreEventAsync(leadId, "INVALID_CONTACT", reason);
            if (result != null) appliedRules.Add("INVALID_CONTACT");
        }

        // 2. Evaluate IRRELEVANT_REQ (-20 pts)
        var reqRedFlags = new List<string>();
        int totalQty = lead.Quantity ?? 0;
        var quantities = lead.GetProductQuantities();
        if (quantities.Any())
        {
            totalQty = quantities.Values.Sum();
        }

        // Absurd quantity check (e.g. 10000 routers for ₹350M from an unverified public form)
        if (totalQty >= 500)
        {
            reqRedFlags.Add($"Absurdly excessive commercial quantity requested ({totalQty} units)");
        }

        // Domain / industry mismatch or spam text
        var domain = (lead.Domain ?? "").ToLowerInvariant();
        var industry = (lead.Industry ?? "").ToLowerInvariant();
        var businessReq = (lead.BusinessRequirement ?? "").ToLowerInvariant();

        if (domain.Contains("school") || domain.Contains("study") || industry.Contains("study") || industry.Contains("student"))
        {
            if (totalQty >= 50 || businessReq.Contains("router") || businessReq.Contains("core") || businessReq.Contains("server"))
            {
                reqRedFlags.Add($"Industry/domain mismatch ({lead.Domain}/{lead.Industry}) for enterprise hardware");
            }
        }

        if (businessReq.Contains("asdf") || businessReq.Contains("free") || businessReq.Contains("give me") || 
            (businessReq.Contains("need these immediately") && totalQty >= 100))
        {
            reqRedFlags.Add("Non-commercial or suspicious requirement phrasing");
        }

        if (reqRedFlags.Any())
        {
            var reason = $"Irrelevant or unrealistic commercial requirement: {string.Join("; ", reqRedFlags)}";
            var result = await ApplyScoreEventAsync(leadId, "IRRELEVANT_REQ", reason);
            if (result != null) appliedRules.Add("IRRELEVANT_REQ");
        }

        return appliedRules;
    }

    public async Task<List<ScoreRule>> GetRulesAsync()
    {
        await EnsureDefaultRulesAsync();
        return await _context.ScoreRules.OrderBy(r => r.Category).ThenByDescending(r => r.Points).ToListAsync();
    }

    public async Task<ScoreRule?> GetRuleByIdAsync(int ruleId)
    {
        return await _context.ScoreRules.FindAsync(ruleId);
    }

    public async Task<ScoreRule> CreateRuleAsync(ScoreRule rule)
    {
        rule.CreatedAt = DateTime.UtcNow;
        rule.UpdatedAt = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(rule.Category)) rule.Category = "INTENT";
        if (string.IsNullOrWhiteSpace(rule.Direction)) rule.Direction = rule.Points >= 0 ? "POSITIVE" : "NEGATIVE";

        _context.ScoreRules.Add(rule);
        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task<ScoreRule> UpdateRuleAsync(int ruleId, int points, bool isActive)
    {
        var rule = await _context.ScoreRules.FindAsync(ruleId);
        if (rule == null) throw new ArgumentException($"Score rule {ruleId} not found");

        rule.Points = points;
        rule.IsActive = isActive;
        rule.Direction = points >= 0 ? "POSITIVE" : "NEGATIVE";
        rule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task<ScoreRule> UpdateRuleFullAsync(int ruleId, ScoreRule updated)
    {
        var rule = await _context.ScoreRules.FindAsync(ruleId);
        if (rule == null) throw new ArgumentException($"Score rule {ruleId} not found");

        rule.Name = updated.Name;
        rule.EventType = updated.EventType;
        rule.Category = updated.Category;
        rule.Direction = updated.Direction;
        rule.Points = updated.Points;
        rule.IsActive = updated.IsActive;
        rule.Description = updated.Description;
        rule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task<ScoreRule> ToggleRuleAsync(int ruleId)
    {
        var rule = await _context.ScoreRules.FindAsync(ruleId);
        if (rule == null) throw new ArgumentException($"Score rule {ruleId} not found");

        rule.IsActive = !rule.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return rule;
    }

    public async Task<bool> DeleteRuleAsync(int ruleId)
    {
        var rule = await _context.ScoreRules.FindAsync(ruleId);
        if (rule == null) return false;

        _context.ScoreRules.Remove(rule);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<LeadScoreHistory?> ApplyScoreEventAsync(int leadId, string eventType, string? customReason = null, int? customDelta = null, bool allowDuplicates = false)
    {
        await EnsureDefaultRulesAsync();

        var lead = await _context.Leads.FindAsync(leadId);
        if (lead == null) return null;

        var rule = await _context.ScoreRules.FirstOrDefaultAsync(r => r.EventType == eventType && r.IsActive);
        if (rule == null && customDelta == null)
        {
            _logger.LogInformation("Score rule for event {EventType} is inactive or not found, and no custom delta provided. Skipping.", eventType);
            return null;
        }

        int delta = customDelta ?? (rule?.Points ?? 0);
        string ruleName = rule?.Name ?? eventType;

        // 1. AI Analysis Single-Count / Idempotency Handling
        // Ensures that running AI analysis multiple times applies the score ONLY ONCE
        bool isAiEvent = eventType == "AI_ANALYSIS" || eventType == "AI_ANALYSIS_NEGATIVE" || eventType == "AI_ANALYSIS_POSITIVE";
        if (isAiEvent)
        {
            var priorAiScore = await _context.LeadScoreHistories
                .Where(h => h.LeadId == leadId && 
                    (h.EventType == "AI_ANALYSIS" || h.EventType == "AI_ANALYSIS_NEGATIVE" || h.EventType == "AI_ANALYSIS_POSITIVE"))
                .OrderByDescending(h => h.Timestamp)
                .FirstOrDefaultAsync();

            if (priorAiScore != null)
            {
                if (priorAiScore.EventType == eventType)
                {
                    // Lead was already scored for this exact AI outcome — apply single time only
                    _logger.LogInformation("Lead {LeadId} has already been scored for AI analysis ({EventType}). Skipping duplicate.", leadId, eventType);
                    return priorAiScore;
                }
                else
                {
                    // Outcome changed (e.g. from POSITIVE to NEGATIVE or vice versa)
                    // Adjust the score by replacing the previous AI delta with the new one
                    int currentScore = lead.Score ?? 0;
                    int netDelta = delta - priorAiScore.Delta;
                    int newScore = Math.Max(0, currentScore + netDelta);
                    lead.Score = newScore;
                    lead.UpdatedAt = DateTime.UtcNow;

                    var updateHistory = new LeadScoreHistory
                    {
                        LeadId = leadId,
                        RuleId = rule?.ScoreRuleId,
                        RuleName = ruleName,
                        EventType = eventType,
                        Delta = delta,
                        TotalScore = newScore,
                        Reason = customReason ?? (rule?.Description ?? $"AI Analysis updated from {priorAiScore.EventType} ({priorAiScore.Delta:+#;-#;0}) to {eventType} ({delta:+#;-#;0})"),
                        Timestamp = DateTime.UtcNow
                    };

                    _context.LeadScoreHistories.Add(updateHistory);
                    await _context.SaveChangesAsync();
                    return updateHistory;
                }
            }
        }
        else if (!allowDuplicates)
        {
            // 2. Generic Single-Count / Idempotency Handling for any rule:
            // "if any lead is doing same thing ... then lead rule must apply only single time"
            bool alreadyApplied = await _context.LeadScoreHistories
                .AnyAsync(h => h.LeadId == leadId && h.EventType == eventType);

            if (alreadyApplied)
            {
                _logger.LogInformation("Event {EventType} already applied for Lead {LeadId}. Skipping duplicate score count.", eventType, leadId);
                return null;
            }
        }

        int currentTotal = lead.Score ?? 0;
        int updatedTotal = Math.Max(0, currentTotal + delta);
        lead.Score = updatedTotal;
        lead.UpdatedAt = DateTime.UtcNow;

        var history = new LeadScoreHistory
        {
            LeadId = leadId,
            RuleId = rule?.ScoreRuleId,
            RuleName = ruleName,
            EventType = eventType,
            Delta = delta,
            TotalScore = updatedTotal,
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

    public async Task<List<string>> GetDistinctEventTypesAsync()
    {
        return await _context.ScoreRules
            .Select(r => r.EventType)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();
    }

    /// <summary>
    /// Returns the list of predefined system event types that do NOT yet have a
    /// ScoreRule configured in the database. This is what the "Add Rule" modal
    /// should populate its event-type dropdown with.
    /// </summary>
    public static readonly IReadOnlyList<string> PredefinedEventTypes = new[]
    {
        "AI_ANALYSIS",
        "AI_ANALYSIS_NEGATIVE",
        "COMPANY_FIT",
        "EMAIL_BOUNCE",
        "EMAIL_CLICK",
        "EMAIL_OPEN",
        "EMAIL_REPLY",
        "FORM_SUBMIT",
        "INTEREST_CLICK",
        "INVALID_CONTACT",
        "IRRELEVANT_REQ",
        "LINKEDIN_ENRICHED",
        "PRODUCT_VIEW",
        "REPEAT_VISIT",
        "ROLE_MATCH"
    };

    public async Task<List<string>> GetUndefinedEventTypesAsync()
    {
        var usedEventTypes = await _context.ScoreRules
            .Select(r => r.EventType.ToUpper())
            .Distinct()
            .ToListAsync();

        var usedSet = new HashSet<string>(usedEventTypes, StringComparer.OrdinalIgnoreCase);

        return PredefinedEventTypes
            .Where(et => !usedSet.Contains(et))
            .OrderBy(et => et)
            .ToList();
    }
}
