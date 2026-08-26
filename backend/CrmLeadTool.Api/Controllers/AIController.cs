using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AIController : ControllerBase
{
    private readonly GroqAIService _aiService;

    public AIController(GroqAIService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>
    /// Analyze a lead using Groq - generates both profile and insights
    /// </summary>
    [HttpPost("analyze/{leadId}")]
    public async Task<IActionResult> AnalyzeLead(int leadId)
    {
        try
        {
            var result = await _aiService.AnalyzeLeadWithProfileAsync(leadId);
            
            return Ok(new
            {
                success = true,
                leadId = leadId,
                profile = new
                {
                    result.Profile.ProfessionalSummary,
                    result.Profile.LikelyIndustry,
                    result.Profile.CompanySize,
                    result.Profile.LikelyLocation,
                    result.Profile.PotentialRole
                },
                analysis = new
                {
                    result.Analysis.Intent,
                    result.Analysis.ConfidenceScore,
                    result.Analysis.LeadSummary,
                    result.Analysis.PriorityRecommendation,
                    result.Analysis.RecommendedNextAction,
                    //result.Analysis.RecommendedSalesperson,
                    insights = result.Analysis.Insights.Select(i => new
                    {
                        i.InsightType,
                        i.InsightText,
                        i.ConfidenceScore
                    })
                },
                analyzedAt = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get AI analysis for a lead
    /// </summary>
    [HttpGet("lead/{leadId}")]
    public async Task<IActionResult> GetAnalysisByLead(int leadId)
    {
        try
        {
            var analysis = await _aiService.GetAnalysisByLeadIdAsync(leadId);
            
            if (analysis == null)
                return NotFound(new { error = $"No AI analysis found for Lead ID {leadId}" });

            return Ok(new AIAnalysisResponseDto
            {
                AIAnalysisId = analysis.AIAnalysisId,
                LeadId = analysis.LeadId,
                Intent = analysis.Intent,
                ConfidenceScore = analysis.ConfidenceScore,
                LeadSummary = analysis.LeadSummary,
                PriorityRecommendation = analysis.PriorityRecommendation,
                RecommendedNextAction = analysis.RecommendedNextAction,
                //RecommendedSalesperson = analysis.RecommendedSalesperson,
                ProfessionalSummary = analysis.ProfessionalSummary,
                LikelyIndustry = analysis.LikelyIndustry,
                CompanySize = analysis.CompanySize,
                LikelyLocation = analysis.LikelyLocation,
                PotentialRole = analysis.PotentialRole,
                AnalysisDate = analysis.AnalysisDate,
                Insights = analysis.Insights.Select(i => new AIInsightDto
                {
                    AIInsightId = i.AIInsightId,
                    InsightType = i.InsightType,
                    InsightText = i.InsightText,
                    ConfidenceScore = i.ConfidenceScore,
                    IsAccepted = i.IsAccepted,
                    IsUsed = i.IsUsed
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get analysis history for a lead
    /// </summary>
    [HttpGet("history/{leadId}")]
    public async Task<IActionResult> GetAnalysisHistory(int leadId)
    {
        try
        {
            var history = await _aiService.GetAnalysisHistoryAsync(leadId);
            return Ok(history.Select(h => new
            {
                h.AIAnalysisHistoryId,
                h.LeadId,
                h.PreviousIntent,
                h.NewIntent,
                h.PreviousPriority,
                h.NewPriority,
                h.ChangedAt,
                h.ChangedBy,
                h.Reason
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all AI analyses
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllAnalyses()
    {
        try
        {
            var analyses = await _aiService.GetAllAnalysesAsync();
            return Ok(analyses.Select(a => new AIAnalysisResponseDto
            {
                AIAnalysisId = a.AIAnalysisId,
                LeadId = a.LeadId,
                Intent = a.Intent,
                ConfidenceScore = a.ConfidenceScore,
                LeadSummary = a.LeadSummary,
                PriorityRecommendation = a.PriorityRecommendation,
                RecommendedNextAction = a.RecommendedNextAction,
               // RecommendedSalesperson = a.RecommendedSalesperson,
                ProfessionalSummary = a.ProfessionalSummary,
                LikelyIndustry = a.LikelyIndustry,
                CompanySize = a.CompanySize,
                LikelyLocation = a.LikelyLocation,
                PotentialRole = a.PotentialRole,
                AnalysisDate = a.AnalysisDate,
                Insights = a.Insights.Select(i => new AIInsightDto
                {
                    AIInsightId = i.AIInsightId,
                    InsightType = i.InsightType,
                    InsightText = i.InsightText,
                    ConfidenceScore = i.ConfidenceScore,
                    IsAccepted = i.IsAccepted,
                    IsUsed = i.IsUsed
                }).ToList()
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}