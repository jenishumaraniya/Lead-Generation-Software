using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Models;
using CrmLeadTool.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CrmLeadTool.Api.Workers;

public class EmailSchedulerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailSchedulerWorker> _logger;

    public EmailSchedulerWorker(IServiceProvider serviceProvider, ILogger<EmailSchedulerWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailSchedulerWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessScheduledEmails(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Transient error occurred while processing scheduled emails. Will retry in next interval.");
            }

            try
            {
                // Poll every 5 seconds for immediate real-time transition and sequence dispatch
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessScheduledEmails(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

        var now = DateTime.UtcNow;

        // 1. Safeguard: Revert any campaigns erroneously marked ACTIVE whose start date is strictly in the future to FUTURE
        var futureCampaignsMarkedActive = await context.Campaigns
            .Where(c => c.Status == "ACTIVE" && c.ScheduleStartDate.HasValue && c.ScheduleStartDate.Value > now)
            .ToListAsync(stoppingToken);

        if (futureCampaignsMarkedActive.Any())
        {
            foreach (var c in futureCampaignsMarkedActive)
            {
                c.Status = "FUTURE";
                c.UpdatedAt = now;
                _logger.LogInformation("Campaign {CampaignId} ({Name}) has future start date ({StartDate} UTC). Corrected status to FUTURE.", c.CampaignId, c.Name, c.ScheduleStartDate);
            }
            await context.SaveChangesAsync(stoppingToken);
        }

        // 2. Transition all FUTURE campaigns whose scheduled start date has arrived into ACTIVE
        var dueFutureCampaigns = await context.Campaigns
            .Where(c => c.Status == "FUTURE" && c.ScheduleStartDate.HasValue && c.ScheduleStartDate.Value <= now && (!c.ScheduleEndDate.HasValue || c.ScheduleEndDate.Value >= now))
            .ToListAsync(stoppingToken);

        if (dueFutureCampaigns.Any())
        {
            foreach (var c in dueFutureCampaigns)
            {
                c.Status = "ACTIVE";
                c.UpdatedAt = now;
                _logger.LogInformation("Campaign {CampaignId} ({Name}) scheduled start reached. Status transitioned to ACTIVE.", c.CampaignId, c.Name);
            }
            await context.SaveChangesAsync(stoppingToken);
        }

        // 3. Transition any expired campaigns
        var expiredCampaigns = await context.Campaigns
            .Where(c => c.ScheduleEndDate.HasValue && c.ScheduleEndDate.Value < now && (c.Status == "ACTIVE" || c.Status == "FUTURE"))
            .ToListAsync(stoppingToken);

        if (expiredCampaigns.Any())
        {
            foreach (var c in expiredCampaigns)
            {
                c.Status = "EXPIRED";
                c.UpdatedAt = now;
                _logger.LogInformation("Campaign {CampaignId} ({Name}) scheduled end reached. Status transitioned to EXPIRED.", c.CampaignId, c.Name);
            }
            await context.SaveChangesAsync(stoppingToken);
        }

        // 4. Automatically process recipients for campaigns that are strictly ACTIVE right now
        var recipients = await context.CampaignRecipients
            .Include(cr => cr.Campaign)
                .ThenInclude(c => c.Steps)
            .Include(cr => cr.EmailMessages)
            .Where(cr => (cr.Status == "ENROLLED" || cr.Status == "IN_PROGRESS")
                      && cr.Campaign != null
                      && cr.Campaign.Status == "ACTIVE"
                      && (!cr.Campaign.ScheduleStartDate.HasValue || cr.Campaign.ScheduleStartDate.Value <= now)
                      && (!cr.Campaign.ScheduleEndDate.HasValue || cr.Campaign.ScheduleEndDate.Value >= now))
            .ToListAsync(stoppingToken);

        foreach (var recipient in recipients)
        {
            var campaign = recipient.Campaign;
            if (campaign == null) continue;

            // Strict check: Campaign MUST be ACTIVE and within its scheduled start/end dates.
            // If the campaign is in FUTURE or any other state, ZERO emails will be sent.
            bool isCurrentlyActive = campaign.Status == "ACTIVE"
                && (!campaign.ScheduleStartDate.HasValue || campaign.ScheduleStartDate.Value <= now)
                && (!campaign.ScheduleEndDate.HasValue || campaign.ScheduleEndDate.Value >= now);

            if (!isCurrentlyActive)
                continue;

            try
            {
                // Ensure at least one sequence step exists
                if (campaign.Steps == null || !campaign.Steps.Any())
                {
                    var defaultStep = new SequenceStep
                    {
                        CampaignId = recipient.CampaignId,
                        StepNumber = 1,
                        Name = "Initial Outreach",
                        Subject = $"Exploring opportunities with {{Company}} - {campaign.Name}",
                        Body = "<p>Hello {{Name}},</p><p>I noticed your work as {{JobTitle}} at {{Company}}. We provide enterprise solutions tailored for your industry.</p><p>Would you be open to a brief introductory conversation this week?</p><p>Best regards,<br/>Sales & Partnerships Team</p>",
                        DelayDays = 0,
                        DelayHours = 0,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.SequenceSteps.Add(defaultStep);
                    await context.SaveChangesAsync(stoppingToken);
                    campaign.Steps = new List<SequenceStep> { defaultStep };
                }

                var lastSentEmail = recipient.EmailMessages?
                    .Where(e => e.Status == "SENT" || e.Status == "DELIVERED")
                    .OrderByDescending(e => e.SentAt)
                    .FirstOrDefault();

                if (lastSentEmail == null)
                {
                    // Campaign is ACTIVE and prospect has not received a delivered Step 1 email yet -> Send Step 1 email now!
                    var lastAttempt = recipient.EmailMessages?.OrderByDescending(e => e.SentAt).FirstOrDefault();
                    if (lastAttempt != null && lastAttempt.Status == "FAILED" && (now - lastAttempt.SentAt).TotalSeconds < 60)
                    {
                        continue;
                    }

                    await emailService.SendEmailAsync(recipient.CampaignRecipientId);
                    _logger.LogInformation("Campaign {CampaignId} is ACTIVE. Automatically dispatched Step 1 email to prospect {ProspectId} (RecipientId {RecipientId}).", campaign.CampaignId, recipient.ProspectId, recipient.CampaignRecipientId);
                }
                else if (recipient.Status == "IN_PROGRESS")
                {
                    // Subsequent sequence steps based on configured delay
                    var currentStep = recipient.CurrentStep ?? 1;
                    var step = campaign.Steps?.FirstOrDefault(s => s.StepNumber == currentStep);
                    if (step == null) continue;

                    var dueTime = lastSentEmail.SentAt.AddDays(step.DelayDays).AddHours(step.DelayHours);
                    if (now >= dueTime)
                    {
                        var lastAttempt = recipient.EmailMessages?.OrderByDescending(e => e.SentAt).FirstOrDefault();
                        if (lastAttempt != null && lastAttempt.Status == "FAILED" && (now - lastAttempt.SentAt).TotalSeconds < 60)
                        {
                            continue;
                        }

                        await emailService.SendEmailAsync(recipient.CampaignRecipientId);
                        _logger.LogInformation("Dispatched sequence step {Step} email to prospect {ProspectId} (RecipientId {RecipientId}) for active campaign {CampaignId}.", currentStep, recipient.ProspectId, recipient.CampaignRecipientId, campaign.CampaignId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching campaign email for recipient {RecipientId} (Prospect {ProspectId}) in active campaign {CampaignId}", recipient.CampaignRecipientId, recipient.ProspectId, recipient.CampaignId);
            }
        }
    }
}