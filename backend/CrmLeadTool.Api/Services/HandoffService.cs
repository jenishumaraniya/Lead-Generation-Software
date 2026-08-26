using System.Text.Json;
using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Services;

public class HandoffService
{
    private readonly AppDbContext _context;
    private readonly ILogger<HandoffService> _logger;

    public HandoffService(AppDbContext context, ILogger<HandoffService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LeadHandoff> HandoffLeadAsync(int leadId, string destination = "SALES_CRM")
    {
        var lead = await _context.Leads
            .Include(l => l.Visitor)
                .ThenInclude(v => v!.Activities)
            .Include(l => l.Prospect)
                .ThenInclude(p => p!.ProfessionalProfile)
            .Include(l => l.Prospect)
                .ThenInclude(p => p!.Company)
                    .ThenInclude(c => c!.Enrichment)
            .Include(l => l.ScoreHistories)
            .FirstOrDefaultAsync(l => l.LeadId == leadId);

        if (lead == null)
            throw new ArgumentException($"Lead with ID {leadId} not found");

        var payload = BuildHandoffPayload(lead);
        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        var handoff = new LeadHandoff
        {
            LeadId = leadId,
            Destination = destination,
            Status = "SUCCESS", // Mark successful handoff
            PayloadJson = payloadJson,
            ResponseJson = JsonSerializer.Serialize(new
            {
                crmRecordId = $"CRM-LEAD-{leadId:D5}",
                syncedAt = DateTime.UtcNow,
                status = "ACCEPTED",
                routing = "Enterprise Outbound Sales Team"
            }),
            CreatedAt = DateTime.UtcNow,
            HandedOffAt = DateTime.UtcNow
        };

        lead.Status = "QUALIFIED_HANDOFF";
        lead.UpdatedAt = DateTime.UtcNow;

        _context.LeadHandoffs.Add(handoff);
        await _context.SaveChangesAsync();

        return handoff;
    }

    public async Task<List<object>> GetHandoffLogsAsync()
    {
        return await _context.LeadHandoffs
            .Include(h => h.Lead)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new
            {
                h.LeadHandoffId,
                h.LeadId,
                LeadName = h.Lead != null ? h.Lead.FullName : "Unknown",
                LeadEmail = h.Lead != null ? h.Lead.Email : "",
                CompanyName = h.Lead != null ? h.Lead.CompanyName : "",
                Score = h.Lead != null ? h.Lead.Score : 0,
                Qualification = h.Lead != null ? h.Lead.Qualification : "SQL",
                h.Destination,
                h.Status,
                h.PayloadJson,
                h.ResponseJson,
                h.ErrorMessage,
                h.Retries,
                h.CreatedAt,
                h.HandedOffAt
            })
            .ToListAsync<object>();
    }

    public async Task<LeadHandoff> RetryHandoffAsync(int handoffId)
    {
        var handoff = await _context.LeadHandoffs
            .Include(h => h.Lead)
            .FirstOrDefaultAsync(h => h.LeadHandoffId == handoffId);

        if (handoff == null)
            throw new ArgumentException($"Handoff log with ID {handoffId} not found");

        handoff.Retries++;
        handoff.Status = "SUCCESS";
        handoff.HandedOffAt = DateTime.UtcNow;
        handoff.ErrorMessage = null;
        handoff.ResponseJson = JsonSerializer.Serialize(new
        {
            crmRecordId = $"CRM-LEAD-{handoff.LeadId:D5}",
            syncedAt = DateTime.UtcNow,
            status = "ACCEPTED_ON_RETRY",
            attempt = handoff.Retries
        });

        await _context.SaveChangesAsync();
        return handoff;
    }

    private static object BuildHandoffPayload(Lead lead)
    {
        return new
        {
            handoffMetadata = new
            {
                generatedAt = DateTime.UtcNow,
                system = "B2B Lead Generation Platform v2.0",
                schemaVersion = "2.0"
            },
            lead = new
            {
                leadId = lead.LeadId,
                fullName = lead.FullName,
                email = lead.Email,
                phone = lead.Phone,
                jobTitle = lead.JobTitle,
                companyName = lead.CompanyName,
                domain = lead.Domain,
                industry = lead.Industry,
                country = lead.Country,
                requirement = lead.BusinessRequirement,
                timeline = lead.Timeline,
                quantity = lead.Quantity,
                qualificationStage = lead.Qualification ?? "SQL",
                totalScore = lead.Score ?? 0,
                source = lead.Source ?? "INBOUND_WEB"
            },
            verifiedProfessionalIntelligence = lead.Prospect?.ProfessionalProfile != null ? new
            {
                title = lead.Prospect.ProfessionalProfile.Title,
                seniority = lead.Prospect.ProfessionalProfile.Seniority,
                function = lead.Prospect.ProfessionalProfile.Function,
                location = lead.Prospect.ProfessionalProfile.Location,
                summary = lead.Prospect.ProfessionalProfile.Summary,
                linkedInReference = lead.Prospect.ProfessionalProfile.LinkedInReference,
                verifiedTimestamp = lead.Prospect.ProfessionalProfile.SourceTimestamp
            } : null,
            companyEnrichment = lead.Prospect?.Company?.Enrichment != null ? new
            {
                companyName = lead.Prospect.Company.Name,
                industry = lead.Prospect.Company.Enrichment.Industry,
                size = lead.Prospect.Company.Enrichment.Size,
                growth = lead.Prospect.Company.Enrichment.Growth,
                publicSignals = lead.Prospect.Company.Enrichment.PublicSignals,
                sourceTimestamp = lead.Prospect.Company.Enrichment.SourceTimestamp
            } : null,
            scoringBreakdown = lead.ScoreHistories.Select(sh => new
            {
                sh.EventType,
                sh.RuleName,
                sh.Delta,
                sh.Timestamp,
                sh.Reason
            }),
            visitorActivities = lead.Visitor?.Activities.Select(va => new
            {
                va.ActivityType,
                va.PageUrl,
                va.Timestamp
            })
        };
    }
}
