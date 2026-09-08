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
                // Poll every 20 seconds for responsive email sequence dispatch
                await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
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

        var recipients = await context.CampaignRecipients
            .Include(cr => cr.Campaign)
                .ThenInclude(c => c.Steps)
            .Include(cr => cr.EmailMessages)
            .Where(cr => cr.Status == "ENROLLED" || cr.Status == "IN_PROGRESS")
            .ToListAsync(stoppingToken);

        foreach (var recipient in recipients)
        {
            if (recipient.Campaign == null) continue;
            // Only active or draft (for testing) campaigns
            if (recipient.Campaign.Status == "PAUSED" || recipient.Campaign.Status == "COMPLETED" || recipient.Campaign.Status == "CLOSED")
                continue;

            try
            {
                // Ensure at least one sequence step exists
                if (recipient.Campaign.Steps == null || !recipient.Campaign.Steps.Any())
                {
                    var defaultStep = new SequenceStep
                    {
                        CampaignId = recipient.CampaignId,
                        StepNumber = 1,
                        Name = "Initial Outreach",
                        Subject = $"Exploring opportunities with {{Company}} - {recipient.Campaign.Name}",
                        Body = "<p>Hello {{Name}},</p><p>I noticed your work as {{JobTitle}} at {{Company}}. We provide enterprise solutions tailored for your industry.</p><p>Would you be open to a brief introductory conversation this week?</p><p>Best regards,<br/>Sales & Partnerships Team</p>",
                        DelayDays = 0,
                        DelayHours = 0,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.SequenceSteps.Add(defaultStep);
                    await context.SaveChangesAsync(stoppingToken);
                    recipient.Campaign.Steps = new List<SequenceStep> { defaultStep };
                }

                var lastEmail = recipient.EmailMessages?.OrderByDescending(e => e.SentAt).FirstOrDefault();

                if (lastEmail != null)
                {
                    var currentStep = recipient.CurrentStep ?? 1;
                    var step = recipient.Campaign.Steps.FirstOrDefault(s => s.StepNumber == currentStep);
                    if (step == null) continue;

                    var dueTime = lastEmail.SentAt.AddDays(step.DelayDays).AddHours(step.DelayHours);
                    if (DateTime.UtcNow >= dueTime && recipient.Status != "COMPLETED")
                    {
                        await emailService.SendEmailAsync(recipient.CampaignRecipientId);
                        _logger.LogInformation("Sent scheduled sequence email to prospect {ProspectId} (RecipientId {RecipientId})", recipient.ProspectId, recipient.CampaignRecipientId);
                    }
                }
                else
                {
                    var firstStep = recipient.Campaign.Steps.OrderBy(s => s.StepNumber).FirstOrDefault();
                    if (firstStep != null)
                    {
                        await emailService.SendEmailAsync(recipient.CampaignRecipientId);
                        _logger.LogInformation("Sent first sequence email to prospect {ProspectId} (RecipientId {RecipientId})", recipient.ProspectId, recipient.CampaignRecipientId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process campaign email for recipient {RecipientId} (Prospect {ProspectId})", recipient.CampaignRecipientId, recipient.ProspectId);
            }
        }
    }
}