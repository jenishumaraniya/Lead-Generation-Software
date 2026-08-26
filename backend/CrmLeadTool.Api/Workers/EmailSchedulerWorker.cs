using CrmLeadTool.Api.Data;
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
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessScheduledEmails(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
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
            var lastEmail = recipient.EmailMessages.OrderByDescending(e => e.SentAt).FirstOrDefault();

            if (lastEmail != null)
            {
                var currentStep = recipient.CurrentStep ?? 1;
                var step = recipient.Campaign.Steps.FirstOrDefault(s => s.StepNumber == currentStep);
                if (step == null) continue;

                var dueTime = lastEmail.SentAt.AddDays(step.DelayDays).AddHours(step.DelayHours);
                if (DateTime.UtcNow >= dueTime && recipient.Status != "COMPLETED")
                {
                    try
                    {
                        await emailService.SendEmailAsync(recipient.CampaignRecipientId);
                        _logger.LogInformation("Sent scheduled email to prospect {ProspectId}", recipient.ProspectId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send scheduled email to prospect {ProspectId}", recipient.ProspectId);
                    }
                }
            }
            else
            {
                var firstStep = recipient.Campaign.Steps.OrderBy(s => s.StepNumber).FirstOrDefault();
                if (firstStep != null)
                {
                    try
                    {
                        await emailService.SendEmailAsync(recipient.CampaignRecipientId);
                        _logger.LogInformation("Sent first email to prospect {ProspectId}", recipient.ProspectId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send first email to prospect {ProspectId}", recipient.ProspectId);
                    }
                }
            }
        }
    }
}