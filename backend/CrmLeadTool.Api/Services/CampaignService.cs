using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Services;

public class CampaignService
{
    private readonly AppDbContext _context;

    public CampaignService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Campaign> CreateCampaignAsync(CreateCampaignDto dto)
    {
        var campaign = new Campaign
        {
            Name = dto.Name,
            Description = dto.Description,
            Status = dto.Status ?? "DRAFT",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Campaigns.Add(campaign);
        await _context.SaveChangesAsync();

        foreach (var stepDto in dto.Steps.OrderBy(s => s.StepNumber))
        {
            var step = new SequenceStep
            {
                CampaignId = campaign.CampaignId,
                StepNumber = stepDto.StepNumber,
                Name = stepDto.Name,
                Subject = stepDto.Subject,
                Body = stepDto.Body,
                DelayDays = stepDto.DelayDays,
                DelayHours = stepDto.DelayHours,
                CreatedAt = DateTime.UtcNow
            };
            _context.SequenceSteps.Add(step);
        }

        await _context.SaveChangesAsync();
        return campaign;
    }

    public async Task<List<object>> GetAllCampaignsAsync()
    {
        return await _context.Campaigns
            .Include(c => c.Steps)
            .Include(c => c.Recipients)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.CampaignId,
                c.Name,
                c.Description,
                c.Status,
                c.ScheduleStartDate,
                c.ScheduleEndDate,
                c.CreatedAt,
                c.UpdatedAt,
                StepsCount = c.Steps.Count,
                RecipientsCount = c.Recipients.Count,
                Steps = c.Steps.OrderBy(s => s.StepNumber).Select(s => new
                {
                    s.SequenceStepId,
                    s.CampaignId,
                    s.StepNumber,
                    s.Name,
                    s.Subject,
                    s.Body,
                    s.DelayDays,
                    s.DelayHours,
                    s.IsActive,
                    s.CreatedAt
                })
            })
            .ToListAsync<object>();
    }

    public async Task<object?> GetCampaignAsync(int id)
    {
        return await _context.Campaigns
            .Include(c => c.Steps)
            .Include(c => c.Recipients)
                .ThenInclude(r => r.Prospect)
            .Where(c => c.CampaignId == id)
            .Select(c => new
            {
                c.CampaignId,
                c.Name,
                c.Description,
                c.Status,
                c.ScheduleStartDate,
                c.ScheduleEndDate,
                c.CreatedAt,
                c.UpdatedAt,
                Steps = c.Steps.OrderBy(s => s.StepNumber).Select(s => new
                {
                    s.SequenceStepId,
                    s.CampaignId,
                    s.StepNumber,
                    s.Name,
                    s.Subject,
                    s.Body,
                    s.DelayDays,
                    s.DelayHours,
                    s.IsActive,
                    s.CreatedAt
                }),
                Recipients = c.Recipients.Select(r => new
                {
                    r.CampaignRecipientId,
                    r.ProspectId,
                    r.Status,
                    r.CurrentStep,
                    r.EnrolledAt,
                    r.LastActivityAt,
                    r.CompletedAt,
                    Prospect = new
                    {
                        r.Prospect.Name,
                        r.Prospect.Email,
                        r.Prospect.JobTitle,
                        r.Prospect.CompanyId
                    }
                })
            })
            .FirstOrDefaultAsync();
    }

    public async Task<CampaignRecipient> EnrollProspectAsync(int campaignId, int prospectId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Steps)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);
        if (campaign == null)
            throw new ArgumentException("Campaign not found.");

        var prospect = await _context.Prospects.FindAsync(prospectId);
        if (prospect == null)
            throw new ArgumentException("Prospect not found.");

        // Check suppression list
        var isSuppressed = await _context.Suppressions.AnyAsync(s => s.Email.ToLower() == prospect.Email.ToLower() && s.IsActive);
        if (isSuppressed)
            throw new InvalidOperationException($"Prospect {prospect.Email} is on the suppression list.");

        var existing = await _context.CampaignRecipients
            .FirstOrDefaultAsync(cr => cr.CampaignId == campaignId && cr.ProspectId == prospectId);
        if (existing != null)
            throw new InvalidOperationException("Prospect already enrolled in this campaign.");

        var recipient = new CampaignRecipient
        {
            CampaignId = campaignId,
            ProspectId = prospectId,
            Status = "ENROLLED",
            CurrentStep = 1,
            EnrolledAt = DateTime.UtcNow
        };

        _context.CampaignRecipients.Add(recipient);
        await _context.SaveChangesAsync();
        return recipient;
    }

    public async Task<object> GetCampaignRecipientsAsync(int campaignId)
    {
        return await _context.CampaignRecipients
            .Where(cr => cr.CampaignId == campaignId)
            .Include(cr => cr.Prospect)
            .Select(cr => new
            {
                cr.CampaignRecipientId,
                cr.CampaignId,
                cr.ProspectId,
                cr.Status,
                cr.CurrentStep,
                cr.EnrolledAt,
                cr.LastActivityAt,
                cr.CompletedAt,
                Prospect = new
                {
                    cr.Prospect.Name,
                    cr.Prospect.Email,
                    cr.Prospect.JobTitle,
                    cr.Prospect.CompanyId
                }
            })
            .ToListAsync();
    }

    public async Task<Campaign> PauseCampaignAsync(int campaignId)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null) throw new ArgumentException("Campaign not found.");

        campaign.Status = "PAUSED";
        campaign.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return campaign;
    }

    public async Task<Campaign> ResumeCampaignAsync(int campaignId)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null) throw new ArgumentException("Campaign not found.");

        campaign.Status = "ACTIVE";
        campaign.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return campaign;
    }

    public async Task<CampaignRecipient> UpdateRecipientStatusAsync(int recipientId, string status)
    {
        var recipient = await _context.CampaignRecipients.FindAsync(recipientId);
        if (recipient == null) throw new ArgumentException("Recipient not found.");

        recipient.Status = status;
        recipient.LastActivityAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return recipient;
    }
}