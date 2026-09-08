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

    private static DateTime ToUtcDate(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc) return dt;
        if (dt.Kind == DateTimeKind.Local) return dt.ToUniversalTime();
        return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }

    public async Task<Campaign> CreateCampaignAsync(CreateCampaignDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Campaign name is required.");

        if (!dto.ScheduleStartDate.HasValue || !dto.ScheduleEndDate.HasValue)
            throw new ArgumentException("Both Scheduled Start Date and Scheduled End Date are mandatory.");

        var startDateUtc = ToUtcDate(dto.ScheduleStartDate.Value);
        var endDateUtc = ToUtcDate(dto.ScheduleEndDate.Value);

        if (endDateUtc <= startDateUtc)
            throw new ArgumentException("Scheduled End Date must be strictly after Scheduled Start Date.");

        var initialStatus = dto.Status ?? "DRAFT";
        var now = DateTime.UtcNow;
        if (endDateUtc < now)
        {
            initialStatus = "EXPIRED";
        }
        else if (startDateUtc > now && initialStatus != "DRAFT")
        {
            initialStatus = "FUTURE";
        }
        else if (initialStatus != "DRAFT")
        {
            initialStatus = "ACTIVE";
        }

        var campaign = new Campaign
        {
            Name = dto.Name,
            Description = dto.Description,
            Status = initialStatus,
            ScheduleStartDate = startDateUtc,
            ScheduleEndDate = endDateUtc,
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

        // Enroll initial prospects if provided
        if (dto.ProspectIds != null && dto.ProspectIds.Any())
        {
            foreach (var prospectId in dto.ProspectIds.Distinct())
            {
                await EnrollProspectAsync(campaign.CampaignId, prospectId);
            }
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
                RecipientsCount = c.Recipients.Count(r => r.Status != "UNENROLLED"),
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
                Recipients = c.Recipients.Where(r => r.Status != "UNENROLLED").Select(r => new
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
            .Include(cr => cr.EmailMessages)
            .FirstOrDefaultAsync(cr => cr.CampaignId == campaignId && cr.ProspectId == prospectId);
        if (existing != null)
        {
            if (existing.Status == "UNENROLLED")
            {
                existing.Status = "ENROLLED";
                existing.LastActivityAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
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

        if (campaign.ScheduleEndDate.HasValue && campaign.ScheduleEndDate.Value < DateTime.UtcNow)
        {
            campaign.Status = "EXPIRED";
            campaign.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            throw new InvalidOperationException("Campaign has expired. Please extend the Scheduled End Date to launch or send emails.");
        }

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

        var now = DateTime.UtcNow;

        if (campaign.ScheduleEndDate.HasValue && campaign.ScheduleEndDate.Value < now)
        {
            campaign.Status = "EXPIRED";
            campaign.UpdatedAt = now;
            await _context.SaveChangesAsync();
            throw new InvalidOperationException("Cannot send emails: Campaign has expired. Please extend the Scheduled End Date.");
        }

        if (campaign.ScheduleStartDate.HasValue && campaign.ScheduleStartDate.Value > now)
        {
            campaign.Status = "FUTURE";
            campaign.UpdatedAt = now;
            await _context.SaveChangesAsync();
            throw new InvalidOperationException($"Cannot send emails: Campaign is scheduled for future start ({campaign.ScheduleStartDate.Value:yyyy-MM-dd HH:mm UTC}). Emails can only be sent once the campaign becomes ACTIVE.");
        }

        if (campaign.Status != "ACTIVE")
        {
            throw new InvalidOperationException($"Cannot send emails: Campaign status is currently '{campaign.Status}'. Emails can only be dispatched when the campaign status is ACTIVE.");
        }

        int sentCount = 0;
        // Only send to active recipients who have not received any email yet
        var pendingRecipients = campaign.Recipients
            .Where(r => r.Status != "UNENROLLED" && (r.EmailMessages == null || !r.EmailMessages.Any()))
            .ToList();

        foreach (var recipient in pendingRecipients)
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
            .Where(cr => cr.CampaignId == campaignId && cr.Status != "UNENROLLED")
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

        if (campaign.ScheduleEndDate.HasValue && campaign.ScheduleEndDate.Value < DateTime.UtcNow)
        {
            campaign.Status = "EXPIRED";
            campaign.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            throw new InvalidOperationException("Campaign has expired. Please edit the campaign to extend the Scheduled End Date before resuming.");
        }

        if (campaign.ScheduleStartDate.HasValue && campaign.ScheduleStartDate.Value > DateTime.UtcNow)
        {
            campaign.Status = "FUTURE";
            campaign.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return campaign;
        }

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

    public async Task<Campaign> UpdateCampaignAsync(int campaignId, CreateCampaignDto dto)
    {
        var campaign = await _context.Campaigns
            .Include(c => c.Steps)
            .Include(c => c.Recipients)
                .ThenInclude(r => r.EmailMessages)
            .FirstOrDefaultAsync(c => c.CampaignId == campaignId);
        if (campaign == null)
            throw new ArgumentException("Campaign not found.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Campaign name is required.");

        if (!dto.ScheduleStartDate.HasValue || !dto.ScheduleEndDate.HasValue)
            throw new ArgumentException("Both Scheduled Start Date and Scheduled End Date are mandatory.");

        var startDateUtc = ToUtcDate(dto.ScheduleStartDate.Value);
        var endDateUtc = ToUtcDate(dto.ScheduleEndDate.Value);

        if (endDateUtc <= startDateUtc)
            throw new ArgumentException("Scheduled End Date must be strictly after Scheduled Start Date.");

        // If extended past now and was expired/completed, allow activating or updating status
        var requestedStatus = dto.Status ?? campaign.Status;
        var now = DateTime.UtcNow;
        if (endDateUtc < now)
        {
            requestedStatus = "EXPIRED";
        }
        else if (startDateUtc > now && requestedStatus != "DRAFT")
        {
            requestedStatus = "FUTURE";
        }
        else if (requestedStatus == "FUTURE" && startDateUtc <= now)
        {
            requestedStatus = "ACTIVE";
        }

        // Update basic fields
        campaign.Name = dto.Name;
        if (dto.Description != null) campaign.Description = dto.Description;
        campaign.Status = requestedStatus;
        campaign.ScheduleStartDate = startDateUtc;
        campaign.ScheduleEndDate = endDateUtc;
        campaign.UpdatedAt = now;

        // Update steps in-place without deleting existing steps that are referenced by EmailMessage records
        if (dto.Steps != null && dto.Steps.Any())
        {
            var orderedDtoSteps = dto.Steps.OrderBy(s => s.StepNumber).ToList();
            
            // Update existing steps or add new steps
            foreach (var stepDto in orderedDtoSteps)
            {
                var existingStep = campaign.Steps.FirstOrDefault(s => s.StepNumber == stepDto.StepNumber);
                
                if (existingStep != null)
                {
                    existingStep.Name = string.IsNullOrWhiteSpace(stepDto.Name) ? $"Step {stepDto.StepNumber}" : stepDto.Name;
                    existingStep.Subject = string.IsNullOrWhiteSpace(stepDto.Subject) ? $"Introduction - {campaign.Name}" : stepDto.Subject;
                    existingStep.Body = string.IsNullOrWhiteSpace(stepDto.Body) ? "<p>Hello {{Name}},</p><p>We wanted to reach out regarding solutions for {{Company}}.</p>" : stepDto.Body;
                    existingStep.DelayDays = stepDto.DelayDays;
                    existingStep.DelayHours = stepDto.DelayHours;
                    existingStep.IsActive = true;
                }
                else
                {
                    var newStep = new SequenceStep
                    {
                        CampaignId = campaign.CampaignId,
                        StepNumber = stepDto.StepNumber,
                        Name = string.IsNullOrWhiteSpace(stepDto.Name) ? $"Step {stepDto.StepNumber}" : stepDto.Name,
                        Subject = string.IsNullOrWhiteSpace(stepDto.Subject) ? $"Introduction - {campaign.Name}" : stepDto.Subject,
                        Body = string.IsNullOrWhiteSpace(stepDto.Body) ? "<p>Hello {{Name}},</p><p>We wanted to reach out regarding solutions for {{Company}}.</p>" : stepDto.Body,
                        DelayDays = stepDto.DelayDays,
                        DelayHours = stepDto.DelayHours,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.SequenceSteps.Add(newStep);
                }
            }

            // For steps no longer present in DTO, mark inactive or remove only if not referenced by emails
            var updatedStepNumbers = orderedDtoSteps.Select(s => s.StepNumber).ToHashSet();
            var removedSteps = campaign.Steps.Where(s => !updatedStepNumbers.Contains(s.StepNumber)).ToList();
            foreach (var removedStep in removedSteps)
            {
                var hasEmails = await _context.EmailMessages.AnyAsync(e => e.SequenceStepId == removedStep.SequenceStepId);
                if (hasEmails)
                {
                    removedStep.IsActive = false; // Soft-disable step so existing email records remain valid
                }
                else
                {
                    _context.SequenceSteps.Remove(removedStep);
                }
            }
        }

        // ---- Sync recipients (Keep existing intact, add new ones) ----
        if (dto.ProspectIds != null)
        {
            var currentRecipients = await _context.CampaignRecipients
                .Include(cr => cr.EmailMessages)
                .Where(cr => cr.CampaignId == campaignId)
                .ToListAsync();

            var selectedProspectIdsSet = dto.ProspectIds.ToHashSet();

            // 1. Handle unselected recipients
            var toRemove = currentRecipients
                .Where(cr => !selectedProspectIdsSet.Contains(cr.ProspectId))
                .ToList();

            foreach (var rec in toRemove)
            {
                if (rec.EmailMessages == null || !rec.EmailMessages.Any())
                {
                    _context.CampaignRecipients.Remove(rec);
                }
                else
                {
                    rec.Status = "UNENROLLED"; // Soft remove so past email records are not violated
                }
            }

            // 2. Handle selected recipients who were previously marked UNENROLLED
            var toReEnroll = currentRecipients
                .Where(cr => selectedProspectIdsSet.Contains(cr.ProspectId) && cr.Status == "UNENROLLED")
                .ToList();

            foreach (var rec in toReEnroll)
            {
                rec.Status = "ENROLLED";
                rec.LastActivityAt = DateTime.UtcNow;
            }

            // 3. Handle brand new recipients that are not in currentRecipients
            var existingProspectIds = currentRecipients.Select(cr => cr.ProspectId).ToHashSet();
            var toAddProspectIds = dto.ProspectIds.Where(id => !existingProspectIds.Contains(id)).Distinct().ToList();

            if (toAddProspectIds.Any())
            {
                if (campaign.Status == "PAUSED")
                {
                    throw new InvalidOperationException("Cannot add new prospects while campaign is PAUSED. Please resume or set status to ACTIVE to add new prospects.");
                }

                foreach (var prospectId in toAddProspectIds)
                {
                    await EnrollProspectAsync(campaignId, prospectId);
                }
            }
        }

        await _context.SaveChangesAsync();

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