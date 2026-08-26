using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Services;

public class LeadService
{
    private readonly AppDbContext _context;
    private readonly DuplicateService _duplicateService;
    private readonly ScoringService _scoringService;
    private readonly QualificationService _qualificationService;
    private readonly ILogger<LeadService> _logger;

    public LeadService(
        AppDbContext context,
        DuplicateService duplicateService,
        ScoringService scoringService,
        QualificationService qualificationService,
        ILogger<LeadService> logger)
    {
        _context = context;
        _duplicateService = duplicateService;
        _scoringService = scoringService;
        _qualificationService = qualificationService;
        _logger = logger;
    }

    public async Task<Lead> CreateLeadFromFormAsync(LeadSubmitDto dto)
    {
        // 1. Multi-key deduplication
        var existing = await _duplicateService.FindDuplicateLeadAsync(dto.Email, dto.Phone, dto.FullName, dto.CompanyName);
        if (existing != null)
        {
            // Update existing lead context and append requirement
            existing.BusinessRequirement = $"{existing.BusinessRequirement} | [Update {DateTime.UtcNow:g}]: {dto.BusinessRequirement}";
            if (!string.IsNullOrEmpty(dto.Timeline)) existing.Timeline = dto.Timeline;
            if (dto.Quantity.HasValue) existing.Quantity = dto.Quantity;
            existing.UpdatedAt = DateTime.UtcNow;

            await _scoringService.ApplyScoreEventAsync(existing.LeadId, "REPEAT_VISIT", "Returning lead submitted additional inquiry", 15);
            await _qualificationService.EvaluateQualificationAsync(existing.LeadId);
            await _context.SaveChangesAsync();
            return existing;
        }

        // 2. Link Visitor and Prospect if available
        int? visitorId = null;
        if (!string.IsNullOrEmpty(dto.VisitorId))
        {
            var visitor = await _context.Visitors.FirstOrDefaultAsync(v => v.AnonymousId == dto.VisitorId);
            if (visitor != null)
            {
                visitorId = visitor.VisitorId;
                visitor.LastSeenAt = DateTime.UtcNow;
            }
        }

        int? prospectId = null;
        if (!string.IsNullOrEmpty(dto.ProspectEmail) || !string.IsNullOrEmpty(dto.Email))
        {
            var searchEmail = string.IsNullOrEmpty(dto.ProspectEmail) ? dto.Email : dto.ProspectEmail;
            var prospect = await _duplicateService.FindProspectByEmailAsync(searchEmail);
            if (prospect != null) prospectId = prospect.ProspectId;
        }

        // 3. Create lead record
        var lead = new Lead
        {
            VisitorId = visitorId,
            ProspectId = prospectId,
            CompanyName = dto.CompanyName,
            FullName = dto.FullName,
            Email = dto.Email,
            JobTitle = dto.JobTitle,
            Domain = dto.Domain,
            Industry = dto.Industry,
            Country = dto.Country,
            Phone = dto.Phone,
            Quantity = dto.Quantity,
            Timeline = dto.Timeline,
            BusinessRequirement = dto.BusinessRequirement,
            Source = dto.Source ?? "WEBSITE_FORM",
            Status = "NEW",
            Score = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        lead.SetProductIdList(dto.Products);

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        // 4. Initial scoring & qualification evaluation
        await _scoringService.ApplyScoreEventAsync(lead.LeadId, "FORM_SUBMIT", "Inbound form submitted with commercial requirement");

        var titleLower = (dto.JobTitle ?? "").ToLower();
        if (titleLower.Contains("vp") || titleLower.Contains("director") || titleLower.Contains("head") || titleLower.Contains("chief") || titleLower.Contains("manager"))
        {
            await _scoringService.ApplyScoreEventAsync(lead.LeadId, "ROLE_MATCH", $"Target decision maker role identified: {dto.JobTitle}");
        }

        await _qualificationService.EvaluateQualificationAsync(lead.LeadId);

        return lead;
    }

    public async Task<List<object>> GetAllLeadsAsync()
    {
        return await _context.Leads
            .Include(l => l.Visitor)
            .Include(l => l.Prospect)
            .Include(l => l.ScoreHistories)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new
            {
                l.LeadId,
                l.FullName,
                l.Email,
                l.CompanyName,
                l.JobTitle,
                l.Domain,
                l.Industry,
                l.Country,
                l.Phone,
                l.Quantity,
                l.Timeline,
                l.BusinessRequirement,
                l.Source,
                l.Status,
                l.Score,
                l.Qualification,
                l.CreatedAt,
                l.UpdatedAt,
                ProductIds = l.GetProductIdList(),
                Visitor = l.Visitor != null ? new
                {
                    l.Visitor.AnonymousId,
                    l.Visitor.FirstSeenAt,
                    l.Visitor.LastSeenAt
                } : null,
                Prospect = l.Prospect != null ? new
                {
                    l.Prospect.ProspectId,
                    l.Prospect.Name,
                    l.Prospect.Email,
                    l.Prospect.Status
                } : null
            })
            .ToListAsync<object>();
    }

    public async Task<object?> GetLeadByIdAsync(int id)
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
            .Include(l => l.Handoffs)
            .FirstOrDefaultAsync(l => l.LeadId == id);

        if (lead == null) return null;

        return new
        {
            lead.LeadId,
            lead.FullName,
            lead.Email,
            lead.CompanyName,
            lead.JobTitle,
            lead.Domain,
            lead.Industry,
            lead.Country,
            lead.Phone,
            lead.Quantity,
            lead.Timeline,
            lead.BusinessRequirement,
            lead.Source,
            lead.Status,
            lead.Score,
            lead.Qualification,
            lead.CreatedAt,
            lead.UpdatedAt,
            ProductIds = lead.GetProductIdList(),
            Visitor = lead.Visitor != null ? new
            {
                lead.Visitor.AnonymousId,
                lead.Visitor.FirstSeenAt,
                lead.Visitor.LastSeenAt,
                Activities = lead.Visitor.Activities.OrderByDescending(a => a.Timestamp).Select(a => new
                {
                    a.ActivityId,
                    a.ActivityType,
                    a.PageUrl,
                    a.Timestamp
                })
            } : null,
            Prospect = lead.Prospect != null ? new
            {
                lead.Prospect.ProspectId,
                lead.Prospect.Name,
                lead.Prospect.Email,
                lead.Prospect.JobTitle,
                lead.Prospect.LinkedInUrl,
                ProfessionalProfile = lead.Prospect.ProfessionalProfile != null ? new
                {
                    lead.Prospect.ProfessionalProfile.Title,
                    lead.Prospect.ProfessionalProfile.Seniority,
                    lead.Prospect.ProfessionalProfile.Function,
                    lead.Prospect.ProfessionalProfile.Location,
                    lead.Prospect.ProfessionalProfile.Summary
                } : null,
                Company = lead.Prospect.Company != null ? new
                {
                    lead.Prospect.Company.Name,
                    lead.Prospect.Company.Domain,
                    lead.Prospect.Company.Industry,
                    lead.Prospect.Company.Size,
                    Enrichment = lead.Prospect.Company.Enrichment != null ? new
                    {
                        lead.Prospect.Company.Enrichment.Growth,
                        lead.Prospect.Company.Enrichment.PublicSignals
                    } : null
                } : null
            } : null,
            ScoreHistories = lead.ScoreHistories.OrderByDescending(sh => sh.Timestamp).Select(sh => new
            {
                sh.LeadScoreHistoryId,
                sh.RuleName,
                sh.EventType,
                sh.Delta,
                sh.TotalScore,
                sh.Reason,
                sh.Timestamp
            }),
            Handoffs = lead.Handoffs.OrderByDescending(h => h.CreatedAt).Select(h => new
            {
                h.LeadHandoffId,
                h.Destination,
                h.Status,
                h.HandedOffAt,
                h.Retries,
                h.ErrorMessage
            })
        };
    }

    public async Task<Lead> UpdateLeadAsync(int id, LeadUpdateDto dto)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) throw new ArgumentException("Lead not found.");

        if (!string.IsNullOrEmpty(dto.Status)) lead.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.Qualification)) lead.Qualification = dto.Qualification;
        if (dto.Score.HasValue) lead.Score = dto.Score;

        lead.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return lead;
    }
}