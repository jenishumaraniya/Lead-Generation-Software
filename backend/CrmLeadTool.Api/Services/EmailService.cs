using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;

    public EmailService(IConfiguration config, AppDbContext context)
    {
        _config = config;
        _context = context;
    }

    // Send an email to a specific campaign recipient
    public async Task<EmailMessage> SendEmailAsync(int campaignRecipientId, string? fromEmail = null)
    {
        var recipient = await _context.CampaignRecipients
            .Include(cr => cr.Prospect)
            .Include(cr => cr.Campaign)
                .ThenInclude(c => c.Steps)
            .FirstOrDefaultAsync(cr => cr.CampaignRecipientId == campaignRecipientId);
        if (recipient == null)
            throw new ArgumentException("Campaign recipient not found.");

        // Find the current step
        var currentStepNumber = recipient.CurrentStep ?? 1;
        var step = recipient.Campaign.Steps
            .FirstOrDefault(s => s.StepNumber == currentStepNumber);
        if (step == null)
            throw new InvalidOperationException("No sequence step found for this recipient.");

        // Build email content
        var from = fromEmail ?? _config["Email:From"];
        var to = recipient.Prospect.Email;

        var body = step.Body
            .Replace("{{Name}}", recipient.Prospect.Name)
            .Replace("{{Company}}", recipient.Prospect.Company?.Name ?? "your company");
        var subject = step.Subject
            .Replace("{{Name}}", recipient.Prospect.Name);

        // Embed tracking pixel and link rewriting
        var bodyWithTracking = EmbedTracking(body, recipient.CampaignRecipientId, subject);

        // Create MimeMessage
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(MailboxAddress.Parse(from));
        mimeMessage.To.Add(MailboxAddress.Parse(to));
        mimeMessage.Subject = subject;
        mimeMessage.Body = new TextPart("html") { Text = bodyWithTracking };

        // Record email in DB
        var emailMessage = new EmailMessage
        {
            CampaignRecipientId = campaignRecipientId,
            SequenceStepId = step.SequenceStepId,
            FromEmail = from,
            ToEmail = to,
            Subject = subject,
            Body = body, // store original body without tracking
            SentAt = DateTime.UtcNow,
            Status = "SENT"
        };
        _context.EmailMessages.Add(emailMessage);
        await _context.SaveChangesAsync();

        // Send via SMTP
        try
        {
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["Email:SmtpHost"],
                int.Parse(_config["Email:SmtpPort"]),
                SecureSocketOptions.StartTls
            );
            await smtp.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
            await smtp.SendAsync(mimeMessage);
            await smtp.DisconnectAsync(true);

            // Update recipient status
            recipient.Status = "IN_PROGRESS";
            recipient.LastActivityAt = DateTime.UtcNow;
            // Advance to next step if exists
            var nextStep = recipient.Campaign.Steps
                .FirstOrDefault(s => s.StepNumber == currentStepNumber + 1);
            if (nextStep != null)
            {
                recipient.CurrentStep = currentStepNumber + 1;
            }
            else
            {
                recipient.Status = "COMPLETED";
                recipient.CompletedAt = DateTime.UtcNow;
            }

            emailMessage.Status = "DELIVERED";
            await _context.SaveChangesAsync();
            return emailMessage;
        }
        catch (Exception ex)
        {
            emailMessage.Status = "FAILED";
            await _context.SaveChangesAsync();
            throw new Exception($"Failed to send email: {ex.Message}");
        }
    }

    // Process a webhook event (Mailtrap or any provider)
    public async Task ProcessEmailEventAsync(EmailEventWebhookDto dto)
    {
        if (string.IsNullOrEmpty(dto.ProviderMessageId))
            throw new ArgumentException("ProviderMessageId is required.");

        // Find the email message by ProviderMessageId (we don't store it yet; we'll use our own ID for now)
        // For MVP, we'll find by recipient and sent time (not perfect, but works)
        var emailMessage = await _context.EmailMessages
            .Where(em => em.ToEmail == dto.RecipientEmail && em.SentAt > DateTime.UtcNow.AddDays(-1))
            .OrderByDescending(em => em.SentAt)
            .FirstOrDefaultAsync();

        if (emailMessage == null)
        {
            // If we can't find, we might want to create a dummy event or log.
            // For simplicity, we'll just skip.
            return;
        }

        // Check idempotency (prevent duplicate events)
        if (!string.IsNullOrEmpty(dto.ProviderEventId))
        {
            var exists = await _context.EmailEvents
                .AnyAsync(e => e.ProviderEventId == dto.ProviderEventId);
            if (exists)
                return; // already processed
        }

        // Create event
        var emailEvent = new EmailEvent
        {
            EmailMessageId = emailMessage.EmailMessageId,
            EventType = dto.EventType,
            EventTimestamp = dto.Timestamp ?? DateTime.UtcNow,
            UserAgent = dto.UserAgent,
            IpAddress = dto.IpAddress,
            ClickUrl = dto.ClickUrl,
            Metadata = dto.Metadata,
            ProviderEventId = dto.ProviderEventId
        };
        _context.EmailEvents.Add(emailEvent);

        // Update EmailMessage status based on event
        switch (dto.EventType.ToUpper())
        {
            case "OPEN":
                emailMessage.OpenedAt = emailEvent.EventTimestamp;
                emailMessage.Status = "OPENED";
                break;
            case "CLICK":
                emailMessage.ClickedAt = emailEvent.EventTimestamp;
                emailMessage.Status = "CLICKED";
                break;
            case "REPLY":
                emailMessage.RepliedAt = emailEvent.EventTimestamp;
                emailMessage.Status = "REPLIED";
                // Pause the campaign for this recipient
                var replyRecipient = await _context.CampaignRecipients
                    .FindAsync(emailMessage.CampaignRecipientId);
                if (replyRecipient != null)
                    replyRecipient.Status = "PAUSED";
                break;
            case "BOUNCE":
                emailMessage.Status = "BOUNCED";
                var bounceRecipient = await _context.CampaignRecipients
                    .FindAsync(emailMessage.CampaignRecipientId);
                if (bounceRecipient != null)
                    bounceRecipient.Status = "BOUNCED";
                break;
            case "COMPLAINT":
                emailMessage.Status = "COMPLAINT";
                break;
            case "UNSUBSCRIBE":
                emailMessage.Status = "UNSUBSCRIBE";
                var unsubRecipient = await _context.CampaignRecipients
                    .FindAsync(emailMessage.CampaignRecipientId);
                if (unsubRecipient != null)
                    unsubRecipient.Status = "STOPPED";
                break;
        }

        await _context.SaveChangesAsync();
    }

    // Helper: embed tracking pixel and rewrite links
    private string EmbedTracking(string htmlBody, int recipientId, string subject)
    {
        var trackingId = $"{recipientId}_{DateTime.UtcNow.Ticks}";

        // Tracking pixel
        var pixelUrl = $"{_config["BaseUrl"]}/api/tracking/open?tid={trackingId}";
        var pixelTag = $"<img src=\"{pixelUrl}\" width=\"1\" height=\"1\" style=\"display:none;\" />";

        // Rewrite links
        var linkPattern = @"href\s*=\s*[""'](http[^""']+)[""']";
        var linkReplacement = $"href=\"{_config["BaseUrl"]}/api/tracking/click?tid={trackingId}&url=$1\"";
        var bodyWithLinks = System.Text.RegularExpressions.Regex.Replace(htmlBody, linkPattern, linkReplacement);

        // Insert pixel before </body>
        if (bodyWithLinks.Contains("</body>"))
            bodyWithLinks = bodyWithLinks.Replace("</body>", pixelTag + "</body>");
        else
            bodyWithLinks += pixelTag;

        return bodyWithLinks;
    }
}