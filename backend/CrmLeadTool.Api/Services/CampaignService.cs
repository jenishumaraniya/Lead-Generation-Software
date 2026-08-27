using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CrmLeadTool.Api.Services;

public class CampaignService
{
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<CampaignService> _logger;

    public CampaignService(AppDbContext context, EmailService emailService, ILogger<CampaignService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
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

        if (dto.Steps != null && dto.Steps.Any())
        {
            foreach (var stepDto in dto.Steps.OrderBy(s => s.StepNumber))
            {
                var step = new SequenceStep
                {
                    CampaignId = campaign.CampaignId,
                    StepNumber = stepDto.StepNumber,
                    Name = string.IsNullOrWhiteSpace(stepDto.Name) ? $"Step {stepDto.StepNumber}" : stepDto.Name,
                    Subject = string.IsNullOrWhiteSpace(stepDto.Subject) ? $"Introduction regarding enterprise solutions - {campaign.Name}" : stepDto.Subject,
                    Body = string.IsNullOrWhiteSpace(stepDto.Body) ? "<p>Hello {{Name}},</p><p>We wanted to reach out regarding our solutions for {{Company}}.</p><p>Best regards,<br/>Sales & Partnerships Team</p>" : stepDto.Body,
                    DelayDays = stepDto.DelayDays,
                    DelayHours = stepDto.DelayHours,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SequenceSteps.Add(step);
            }

            await _context.SaveChangesAsync();
        }
        else
        {
            // Auto-create default Step 1
            var defaultStep = new SequenceStep
            {
                CampaignId = campaign.CampaignId,
                StepNumber = 1,
                Name = "Initial Outreach",
                Subject = $"Exploring opportunities with {{Company}} - {campaign.Name}",
                Body = "<p>Hello {{Name}},</p><p>I noticed your work as {{JobTitle}} at {{Company}}. We provide enterprise solutions tailored for your industry.</p><p>Would you be open to a brief introductory conversation this week?</p><p>Best regards,<br/>Sales & Partnerships Team</p>",
                DelayDays = 0,
                DelayHours = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.SequenceSteps.Add(defaultStep);
            await _context.SaveChangesAsync();
        }

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

    public async Task<CampaignRecipient> EnrollProspectAsync(int campaignId, int prospectId, bool sendImmediately = false)
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
        {
            if (sendImmediately || campaign.Status == "ACTIVE")
            {
                try
                {
                    await _emailService.SendEmailAsync(existing.CampaignRecipientId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not send email for existing recipient {Id}", existing.CampaignRecipientId);
                }
            }
            return existing;
        }

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

        // If campaign is ACTIVE or immediate send requested, trigger Step 1 email right away
        if (campaign.Status == "ACTIVE" || sendImmediately)
        {
            try
            {
                await _emailService.SendEmailAsync(recipient.CampaignRecipientId);
                _logger.LogInformation("Sent initial campaign email to prospect {Email} (RecipientId {Id})", prospect.Email, recipient.CampaignRecipientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send initial campaign email for recipient {RecipientId}", recipient.CampaignRecipientId);
            }
        }

        return recipient;
    }

    public async Task<int> LaunchCampaignEmailsAsync(int campaignId)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Steps)
            .Include(c => c.Recipients)
                .ThenInclude(r => r.EmailMessages)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);

        if (campaign == null) throw new ArgumentException("Campaign not found.");

        // Ensure at least 1 step exists
        if (!campaign.Steps.Any())
        {
            var defaultStep = new SequenceStep
            {
                CampaignId = campaign.CampaignId,
                StepNumber = 1,
                Name = "Initial Outreach",
                Subject = $"Exploring opportunities with {{Company}} - {campaign.Name}",
                Body = "<p>Hello {{Name}},</p><p>I noticed your work as {{JobTitle}} at {{Company}}. We provide enterprise solutions tailored for your industry.</p><p>Would you be open to a brief introductory conversation this week?</p><p>Best regards,<br/>Sales & Partnerships Team</p>",
                DelayDays = 0,
                DelayHours = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.SequenceSteps.Add(defaultStep);
            await _context.SaveChangesAsync();
            campaign.Steps.Add(defaultStep);
        }

        campaign.Status = "ACTIVE";
        campaign.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        int sentCount = 0;
        foreach (var recipient in campaign.Recipients.Where(r => r.Status == "ENROLLED" || r.EmailMessages == null || !r.EmailMessages.Any()))
        {
            try
            {
                await _emailService.SendEmailAsync(recipient.CampaignRecipientId);
                sentCount++;
                _logger.LogInformation("Immediately launched email for recipient {RecipientId} in campaign {CampaignId}", recipient.CampaignRecipientId, campaignId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send immediate email for recipient {RecipientId}", recipient.CampaignRecipientId);
            }
        }

        return sentCount;
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

        // Also launch pending emails
        await LaunchCampaignEmailsAsync(campaignId);
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

    public async Task<Campaign> UpdateCampaignAsync(int campaignId, CreateCampaignDto dto)
    {
        var campaign = await _context.Campaigns.Include(c => c.Steps).FirstOrDefaultAsync(c => c.CampaignId == campaignId);
        if (campaign == null) throw new ArgumentException("Campaign not found.");

        if (!string.IsNullOrEmpty(dto.Name)) campaign.Name = dto.Name;
        if (dto.Description != null) campaign.Description = dto.Description;
        if (!string.IsNullOrEmpty(dto.Status)) campaign.Status = dto.Status;
        campaign.ScheduleStartDate = dto.ScheduleStartDate;
        campaign.ScheduleEndDate = dto.ScheduleEndDate;
        campaign.UpdatedAt = DateTime.UtcNow;

        if (dto.Steps != null && dto.Steps.Any())
        {
            _context.SequenceSteps.RemoveRange(campaign.Steps);
            foreach (var stepDto in dto.Steps.OrderBy(s => s.StepNumber))
            {
                campaign.Steps.Add(new SequenceStep
                {
                    CampaignId = campaign.CampaignId,
                    StepNumber = stepDto.StepNumber,
                    Name = string.IsNullOrWhiteSpace(stepDto.Name) ? $"Step {stepDto.StepNumber}" : stepDto.Name,
                    Subject = string.IsNullOrWhiteSpace(stepDto.Subject) ? $"Introduction - {campaign.Name}" : stepDto.Subject,
                    Body = string.IsNullOrWhiteSpace(stepDto.Body) ? "<p>Hello {{Name}},</p><p>We wanted to reach out regarding solutions for {{Company}}.</p>" : stepDto.Body,
                    DelayDays = stepDto.DelayDays,
                    DelayHours = stepDto.DelayHours,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        // If campaign is updated to ACTIVE, immediately dispatch pending emails
        if (campaign.Status == "ACTIVE")
        {
            await LaunchCampaignEmailsAsync(campaignId);
        }

        return campaign;
    }

    public async Task<Campaign> CloseCampaignAsync(int campaignId)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null) throw new ArgumentException("Campaign not found.");

        campaign.Status = "COMPLETED";
        campaign.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return campaign;
    }

    public async Task DeleteCampaignAsync(int campaignId)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null) throw new ArgumentException("Campaign not found.");

        _context.Campaigns.Remove(campaign);
        await _context.SaveChangesAsync();
    }
}