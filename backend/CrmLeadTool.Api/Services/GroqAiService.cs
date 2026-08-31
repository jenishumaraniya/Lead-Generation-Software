using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace CrmLeadTool.Api.Services;

public class GroqAIService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;
    private readonly ILogger<GroqAIService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ScoringService _scoringService;   // 👈 NEW

    public GroqAIService(
        IConfiguration config,
        AppDbContext context,
        ILogger<GroqAIService> logger,
        HttpClient httpClient,
        ScoringService scoringService)   // 👈 NEW
    {
        _config = config;
        _context = context;
        _logger = logger;
        _httpClient = httpClient;
        _scoringService = scoringService;   // 👈 NEW
    }

    public async Task<CombinedAIAnalysis> AnalyzeLeadWithProfileAsync(int leadId)
    {
        var lead = await _context.Leads
            .Include(l => l.Visitor)
            .Include(l => l.Prospect)
            .FirstOrDefaultAsync(l => l.LeadId == leadId);

        if (lead == null)
            throw new ArgumentException($"Lead with ID {leadId} not found.");

        var prompt = BuildEnhancedPrompt(lead);
        var response = await CallGroqApiAsync(prompt);
        var result = ParseCombinedResponse(response, leadId);

        await SaveCombinedAnalysisAsync(result, leadId);
        await UpdateLeadWithAIAsync(leadId, result.Analysis);

        // 👇 NEW: Apply scoring points for AI analysis
        await _scoringService.ApplyScoreEventAsync(
            leadId,
            "AI_ANALYSIS",
            "AI lead analysis completed",
            15   // points to add
        );

        return result;
    }

    public async Task<AIAnalysis?> GetAnalysisByLeadIdAsync(int leadId)
    {
        return await _context.AIAnalyses
            .Include(a => a.Insights)
            .FirstOrDefaultAsync(a => a.LeadId == leadId);
    }

    public async Task<List<AIAnalysis>> GetAllAnalysesAsync()
    {
        return await _context.AIAnalyses
            .Include(a => a.Insights)
            .OrderByDescending(a => a.AnalysisDate)
            .ToListAsync();
    }

    public async Task<List<AIAnalysisHistory>> GetAnalysisHistoryAsync(int leadId)
    {
        return await _context.AIAnalysisHistories
            .Where(h => h.LeadId == leadId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();
    }

    #region Private Methods

    private string BuildEnhancedPrompt(Lead lead)
    {
        var activities = _context.LeadActivities
            .Where(a => a.LeadId == lead.LeadId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(3)
            .Select(a => $"- {Truncate(a.ActivityType, 50)}")
            .ToList();

        var activitySummary = activities.Any() 
            ? string.Join("\n", activities) 
            : "- None";

        var businessReq = Truncate(lead.BusinessRequirement ?? "", 200);
        var companyName = Truncate(lead.CompanyName ?? "", 100);
        var fullName = Truncate(lead.FullName ?? "", 100);
        var jobTitle = Truncate(lead.JobTitle ?? "", 100);
        var timeline = Truncate(lead.Timeline ?? "", 100);

        var prompt = $@"Analyze this B2B lead. Return ONLY valid JSON.

Lead: {fullName}
Company: {companyName}
Title: {jobTitle}
Need: {businessReq}
Timeline: {timeline}
Score: {lead.Score}
Activities: {activitySummary}

JSON:
{{
    ""generatedProfile"": {{
        ""professionalSummary"": ""2-3 sentence summary"",
        ""likelyIndustry"": ""industry"",
        ""companySize"": ""Startup/SMB/Mid-Market/Enterprise"",
        ""likelyLocation"": ""location"",
        ""potentialRole"": ""role""
    }},
    ""analysis"": {{
        ""intent"": ""BUYING/RESEARCHING/COMPARING/AWARENESS"",
        ""confidenceScore"": 85,
        ""leadSummary"": ""1 sentence summary"",
        ""priorityRecommendation"": ""URGENT/HIGH/MEDIUM/LOW"",
        ""recommendedNextAction"": ""action"",
        ""painPoints"": [""pain1"", ""pain2""],
        ""icebreakers"": [""ice1"", ""ice2""],
        ""talkingPoints"": [""point1"", ""point2""]
    }}
}}";

        _logger.LogInformation("Prompt length: {Length} characters", prompt.Length);
        return prompt;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.Length <= maxLength) return value;
        return value.Substring(0, maxLength) + "...";
    }

    private async Task<string> CallGroqApiAsync(string prompt)
    {
        var apiKey = _config["Groq:ApiKey"];
        var model = _config["Groq:Model"] ?? "llama3-70b-8192";
        var maxTokens = int.Parse(_config["Groq:MaxTokens"] ?? "400");
        var temperature = double.Parse(_config["Groq:Temperature"] ?? "0.7");

        var payload = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are a B2B sales analyst. Always respond with valid JSON." },
                new { role = "user", content = prompt }
            },
            temperature = temperature,
            max_tokens = maxTokens,
            response_format = new { type = "json_object" }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var response = await _httpClient.PostAsync(
            "https://api.groq.com/openai/v1/chat/completions",
            content
        );

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Groq API error: {StatusCode} - {Response}", response.StatusCode, responseBody);
            throw new Exception($"Groq API error: {response.StatusCode} - {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var result = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return result ?? "{}";
    }

    private CombinedAIAnalysis ParseCombinedResponse(string response, int leadId)
    {
        try
        {
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            var profile = new GeneratedProfile();
            if (root.TryGetProperty("generatedProfile", out var profileElement))
            {
                profile.ProfessionalSummary = profileElement.TryGetProperty("professionalSummary", out var ps) ? ps.GetString() ?? "Professional with relevant experience" : "Professional with relevant experience";
                profile.LikelyIndustry = profileElement.TryGetProperty("likelyIndustry", out var li) ? li.GetString() ?? "Technology" : "Technology";
                profile.CompanySize = profileElement.TryGetProperty("companySize", out var cs) ? cs.GetString() ?? "SMB" : "SMB";
                profile.LikelyLocation = profileElement.TryGetProperty("likelyLocation", out var ll) ? ll.GetString() ?? "United States" : "United States";
                profile.PotentialRole = profileElement.TryGetProperty("potentialRole", out var pr) ? pr.GetString() ?? "Business Decision Maker" : "Business Decision Maker";
            }

            var analysis = new AIAnalysis();
            if (root.TryGetProperty("analysis", out var analysisElement))
            {
                analysis.LeadId = leadId;
                analysis.Intent = analysisElement.TryGetProperty("intent", out var intent) ? intent.GetString() ?? "AWARENESS" : "AWARENESS";
                analysis.ConfidenceScore = analysisElement.TryGetProperty("confidenceScore", out var conf) 
                    ? Convert.ToDecimal(conf.GetDouble()) 
                    : 50m;
                analysis.LeadSummary = analysisElement.TryGetProperty("leadSummary", out var summary) ? summary.GetString() ?? "Lead requires further analysis" : "Lead requires further analysis";
                analysis.PriorityRecommendation = analysisElement.TryGetProperty("priorityRecommendation", out var priority) ? priority.GetString() ?? "MEDIUM" : "MEDIUM";
                analysis.RecommendedNextAction = analysisElement.TryGetProperty("recommendedNextAction", out var action) ? action.GetString() ?? "Contact the lead" : "Contact the lead";
                analysis.AnalysisDate = DateTime.UtcNow;
                analysis.CreatedAt = DateTime.UtcNow;
                analysis.RawResponse = response;
                analysis.ModelVersion = _config["Groq:Model"] ?? "llama3-70b-8192";

                var insights = new List<AIInsight>();

                if (analysisElement.TryGetProperty("painPoints", out var painPoints) && painPoints.ValueKind == JsonValueKind.Array)
                {
                    foreach (var point in painPoints.EnumerateArray())
                    {
                        insights.Add(new AIInsight 
                        { 
                            LeadId = leadId, 
                            InsightType = "PAIN_POINT", 
                            InsightText = point.GetString() ?? "Unknown pain point", 
                            ConfidenceScore = 75m,
                            CreatedAt = DateTime.UtcNow 
                        });
                    }
                }

                if (analysisElement.TryGetProperty("icebreakers", out var icebreakers) && icebreakers.ValueKind == JsonValueKind.Array)
                {
                    foreach (var icebreaker in icebreakers.EnumerateArray())
                    {
                        insights.Add(new AIInsight 
                        { 
                            LeadId = leadId, 
                            InsightType = "ICEBREAKER", 
                            InsightText = icebreaker.GetString() ?? "Unknown icebreaker", 
                            ConfidenceScore = 70m,
                            CreatedAt = DateTime.UtcNow 
                        });
                    }
                }

                if (analysisElement.TryGetProperty("talkingPoints", out var talkingPoints) && talkingPoints.ValueKind == JsonValueKind.Array)
                {
                    foreach (var point in talkingPoints.EnumerateArray())
                    {
                        insights.Add(new AIInsight 
                        { 
                            LeadId = leadId, 
                            InsightType = "TALKING_POINT", 
                            InsightText = point.GetString() ?? "Unknown talking point", 
                            ConfidenceScore = 80m,
                            CreatedAt = DateTime.UtcNow 
                        });
                    }
                }

                analysis.Insights = insights;
            }

            return new CombinedAIAnalysis { Profile = profile, Analysis = analysis, RawResponse = response };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI response: {Response}", response);
            return new CombinedAIAnalysis
            {
                Profile = new GeneratedProfile { ProfessionalSummary = "Unable to generate profile.", LikelyIndustry = "Unknown", CompanySize = "Unknown", LikelyLocation = "Unknown", PotentialRole = "Unknown" },
                Analysis = new AIAnalysis
                {
                    LeadId = leadId,
                    Intent = "AWARENESS",
                    ConfidenceScore = 50m,
                    LeadSummary = "Unable to analyze lead. Please review manually.",
                    PriorityRecommendation = "MEDIUM",
                    RecommendedNextAction = "Contact the lead for more information",
                    AnalysisDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    RawResponse = response,
                    ModelVersion = _config["Groq:Model"] ?? "llama3-70b-8192",
                    Insights = new List<AIInsight>()
                },
                RawResponse = response
            };
        }
    }

    private async Task SaveCombinedAnalysisAsync(CombinedAIAnalysis result, int leadId)
    {
        var analysis = result.Analysis;

        var existing = await _context.AIAnalyses
            .FirstOrDefaultAsync(a => a.LeadId == leadId);

        if (existing != null)
        {
            var history = new AIAnalysisHistory
            {
                LeadId = leadId,
                PreviousIntent = existing.Intent,
                NewIntent = analysis.Intent,
                PreviousPriority = existing.PriorityRecommendation,
                NewPriority = analysis.PriorityRecommendation,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = "AI_SYSTEM",
                Reason = "New combined analysis completed"
            };
            _context.AIAnalysisHistories.Add(history);

            existing.Intent = analysis.Intent;
            existing.ConfidenceScore = analysis.ConfidenceScore;
            existing.LeadSummary = analysis.LeadSummary;
            existing.PriorityRecommendation = analysis.PriorityRecommendation;
            existing.RecommendedNextAction = analysis.RecommendedNextAction;
            existing.ProfessionalSummary = result.Profile.ProfessionalSummary;
            existing.LikelyIndustry = result.Profile.LikelyIndustry;
            existing.CompanySize = result.Profile.CompanySize;
            existing.LikelyLocation = result.Profile.LikelyLocation;
            existing.PotentialRole = result.Profile.PotentialRole;
            existing.AnalysisDate = analysis.AnalysisDate;
            existing.RawResponse = analysis.RawResponse;
            existing.ModelVersion = analysis.ModelVersion;
            existing.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            analysis.ProfessionalSummary = result.Profile.ProfessionalSummary;
            analysis.LikelyIndustry = result.Profile.LikelyIndustry;
            analysis.CompanySize = result.Profile.CompanySize;
            analysis.LikelyLocation = result.Profile.LikelyLocation;
            analysis.PotentialRole = result.Profile.PotentialRole;

            _context.AIAnalyses.Add(analysis);
        }

        await _context.SaveChangesAsync();

        var savedAnalysis = await _context.AIAnalyses
            .FirstOrDefaultAsync(a => a.LeadId == leadId);

        if (savedAnalysis != null && analysis.Insights.Any())
        {
            foreach (var insight in analysis.Insights)
            {
                insight.AIAnalysisId = savedAnalysis.AIAnalysisId;
                _context.AIInsights.Add(insight);
            }
            await _context.SaveChangesAsync();
        }
    }

    private async Task UpdateLeadWithAIAsync(int leadId, AIAnalysis analysis)
    {
        var lead = await _context.Leads.FindAsync(leadId);
        if (lead == null) return;

        if (analysis.PriorityRecommendation != "MEDIUM")
        {
            lead.PriorityLevel = analysis.PriorityRecommendation;
        }

        if (analysis.Intent == "BUYING" && analysis.ConfidenceScore > 70)
        {
            if (string.IsNullOrEmpty(lead.Qualification) || lead.Qualification == "COLD")
            {
                lead.Qualification = "HOT";
            }
        }

        if (!string.IsNullOrEmpty(analysis.LikelyIndustry) && string.IsNullOrEmpty(lead.Industry))
        {
            lead.Industry = analysis.LikelyIndustry;
        }

        lead.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    #endregion
}

public class CombinedAIAnalysis
{
    public GeneratedProfile Profile { get; set; } = new();
    public AIAnalysis Analysis { get; set; } = new();
    public string RawResponse { get; set; } = string.Empty;
}

public class GeneratedProfile
{
    public string ProfessionalSummary { get; set; } = string.Empty;
    public string LikelyIndustry { get; set; } = string.Empty;
    public string CompanySize { get; set; } = string.Empty;
    public string LikelyLocation { get; set; } = string.Empty;
    public string PotentialRole { get; set; } = string.Empty;
}