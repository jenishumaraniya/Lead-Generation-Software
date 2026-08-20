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
            ScheduleStartDate = dto.ScheduleStartDate,
            ScheduleEndDate = dto.ScheduleEndDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Campaigns.Add(campaign);
        await _context.SaveChangesAsync();

        // Add steps
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

    public async Task<List<Campaign>> GetAllCampaignsAsync()
    {
        return await _context.Campaigns
            .Include(c => c.Steps)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Campaign?> GetCampaignAsync(int id)
    {
        return await _context.Campaigns
            .Include(c => c.Steps)
            .Include(c => c.Recipients)
                .ThenInclude(r => r.Prospect)
            .FirstOrDefaultAsync(c => c.CampaignId == id);
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

        // Check if already enrolled
        var existing = await _context.CampaignRecipients
            .FirstOrDefaultAsync(cr => cr.CampaignId == campaignId && cr.ProspectId == prospectId);
        if (existing != null)
            throw new InvalidOperationException("Prospect already enrolled.");

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

    public async Task UpdateCampaignStatusAsync(int campaignId, string status)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null)
            throw new ArgumentException("Campaign not found.");

        campaign.Status = status;
        campaign.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}