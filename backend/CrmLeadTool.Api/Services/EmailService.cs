using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CrmLeadTool.Api.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;
    private readonly ScoringService _scoringService;
    private readonly QualificationService _qualificationService;
    private readonly ILogger<EmailService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public EmailService(
        IConfiguration config,
        AppDbContext context,
        ScoringService scoringService,
        QualificationService qualificationService,
        ILogger<EmailService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _context = context;
        _scoringService = scoringService;
        _qualificationService = qualificationService;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<EmailMessage> SendEmailAsync(int campaignRecipientId, string? fromEmail = null)
    {
        var recipient = await _context.CampaignRecipients
            .Include(cr => cr.Prospect)
                .ThenInclude(p => p.Company)
            .Include(cr => cr.Campaign)
                .ThenInclude(c => c.Steps)
            .FirstOrDefaultAsync(cr => cr.CampaignRecipientId == campaignRecipientId);

        if (recipient == null)
            throw new ArgumentException("Campaign recipient not found.");

        var isSuppressed = await _context.Suppressions.AnyAsync(s => s.Email.ToLower() == recipient.Prospect.Email.ToLower() && s.IsActive);
        if (isSuppressed)
            throw new InvalidOperationException($"Recipient {recipient.Prospect.Email} is suppressed.");

        var currentStepNumber = recipient.CurrentStep ?? 1;
        var step = recipient.Campaign.Steps
            .FirstOrDefault(s => s.StepNumber == currentStepNumber);

        if (step == null)
            throw new InvalidOperationException("No sequence step found.");

        var from = fromEmail ?? _config["Email:From"] ?? "noreply@b2bleadgen.com";
        var to = recipient.Prospect.Email;

        var body = step.Body
            .Replace("{{Name}}", recipient.Prospect.Name)
            .Replace("{{Company}}", recipient.Prospect.Company?.Name ?? "your company")
            .Replace("{{JobTitle}}", recipient.Prospect.JobTitle ?? "Leader");

        var subject = step.Subject
            .Replace("{{Name}}", recipient.Prospect.Name)
            .Replace("{{Company}}", recipient.Prospect.Company?.Name ?? "");

        var bodyWithTracking = EmbedTracking(body, recipient.CampaignRecipientId);

        var emailMessage = new EmailMessage
        {
            CampaignRecipientId = campaignRecipientId,
            SequenceStepId = step.SequenceStepId,
            FromEmail = from,
            ToEmail = to,
            Subject = subject,
            Body = body,
            SentAt = DateTime.UtcNow,
            Status = "SENT"
        };
        _context.EmailMessages.Add(emailMessage);
        await _context.SaveChangesAsync();

        try
        {
            var messageId = await SendViaMailtrapApiAsync(to, subject, bodyWithTracking, from);

            recipient.Status = "IN_PROGRESS";
            recipient.LastActivityAt = DateTime.UtcNow;

            var nextStep = recipient.Campaign.Steps
                .FirstOrDefault(s => s.StepNumber == currentStepNumber + 1);

            if (nextStep != null)
                recipient.CurrentStep = currentStepNumber + 1;
            else
            {
                recipient.Status = "COMPLETED";
                recipient.CompletedAt = DateTime.UtcNow;
            }

            emailMessage.ProviderMessageId = messageId;
            emailMessage.Status = "DELIVERED";
            await _context.SaveChangesAsync();

            return emailMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            emailMessage.Status = "FAILED";
            await _context.SaveChangesAsync();
            throw;
        }
    }

    private async Task<string> SendViaMailtrapApiAsync(string to, string subject, string htmlBody, string fromEmail)
    {
        var apiKey = _config["Mailtrap:ApiKey"];
        var fromName = _config["Mailtrap:FromName"] ?? "LeadGen Platform";
        const string mailtrapUrl = "https://send.api.mailtrap.io/api/send";

        if (string.IsNullOrEmpty(apiKey) || apiKey.StartsWith("YOUR_"))
        {
            // Simulate successful delivery in development mode (no API key configured)
            _logger.LogWarning("Mailtrap API key not configured - simulating delivery.");
            return $"sim-{Guid.NewGuid():N}";
        }

        var payload = new
        {
            from = new { email = fromEmail, name = fromName },
            to = new[] { new { email = to } },
            subject,
            html = htmlBody,
            text = StripHtml(htmlBody)
        };

        var json = JsonSerializer.Serialize(payload);

        // Use IHttpClientFactory — thread-safe, no shared header state
        using var httpClient = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, mailtrapUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Mailtrap API error {StatusCode}: {Response}", response.StatusCode, responseBody);
            throw new Exception($"Mailtrap API error {response.StatusCode}: {responseBody}");
        }

        _logger.LogInformation("Mailtrap email sent to {To}. Response: {Response}", to, responseBody);

        using var doc = JsonDocument.Parse(responseBody);
        if (doc.RootElement.TryGetProperty("message_ids", out var messageIds) && messageIds.EnumerateArray().Any())
            return messageIds.EnumerateArray().First().GetString() ?? $"msg-{Guid.NewGuid():N}";

        return $"msg-{Guid.NewGuid():N}";
    }

    private string EmbedTracking(string htmlBody, int recipientId)
    {
        var trackingId = $"{recipientId}_{DateTime.UtcNow.Ticks}";
        var baseUrl = _config["BaseUrl"] ?? "http://localhost:5234";

        var pixelUrl = $"{baseUrl}/api/tracking/open?tid={trackingId}";
        var pixelTag = $"<img src=\"{pixelUrl}\" width=\"1\" height=\"1\" style=\"display:none;\" />";

        var linkPattern = @"href\s*=\s*[""'](http[^""']+)[""']";
        var linkReplacement = $"href=\"{baseUrl}/api/tracking/click?tid={trackingId}&url=$1\"";
        var bodyWithLinks = System.Text.RegularExpressions.Regex.Replace(htmlBody, linkPattern, linkReplacement);

        if (bodyWithLinks.Contains("</body>"))
            bodyWithLinks = bodyWithLinks.Replace("</body>", pixelTag + "</body>");
        else
            bodyWithLinks += pixelTag;

        return bodyWithLinks;
    }

    public async Task ProcessEmailEventAsync(EmailEventWebhookDto dto)
    {
        var emailMessage = await _context.EmailMessages
            .Include(em => em.CampaignRecipient)
                .ThenInclude(cr => cr.Prospect)
            .FirstOrDefaultAsync(em => em.ProviderMessageId == dto.ProviderMessageId);

        if (emailMessage == null)
        {
            emailMessage = await _context.EmailMessages
                .Include(em => em.CampaignRecipient)
                    .ThenInclude(cr => cr.Prospect)
                .Where(em => em.ToEmail == dto.RecipientEmail && em.SentAt > DateTime.UtcNow.AddDays(-3))
                .OrderByDescending(em => em.SentAt)
                .FirstOrDefaultAsync();
        }

        if (emailMessage == null)
        {
            _logger.LogWarning("Email message not found for: {ProviderId}", dto.ProviderMessageId);
            return;
        }

        // Idempotency check on ProviderEventId
        if (!string.IsNullOrEmpty(dto.ProviderEventId))
        {
            var exists = await _context.EmailEvents
                .AnyAsync(e => e.ProviderEventId == dto.ProviderEventId);
            if (exists) return;
        }

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

        var recipient = emailMessage.CampaignRecipient;
        var prospect = recipient?.Prospect;

        var eventUpper = dto.EventType.ToUpper();
        switch (eventUpper)
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
                if (recipient != null)
                {
                    recipient.Status = "PAUSED"; // Auto-pause sequence on positive engagement
                }
                break;
            case "BOUNCE":
                emailMessage.Status = "BOUNCED";
                if (recipient != null) recipient.Status = "STOPPED";
                if (prospect != null)
                {
                    _context.Suppressions.Add(new Suppression
                    {
                        Email = prospect.Email,
                        ProspectId = prospect.ProspectId,
                        Reason = "BOUNCE",
                        Notes = "Hard bounce recorded via webhook"
                    });
                }
                break;
            case "UNSUBSCRIBE":
                emailMessage.Status = "UNSUBSCRIBE";
                if (recipient != null) recipient.Status = "STOPPED";
                if (prospect != null)
                {
                    _context.Suppressions.Add(new Suppression
                    {
                        Email = prospect.Email,
                        ProspectId = prospect.ProspectId,
                        Reason = "OPT_OUT",
                        Notes = "Unsubscribe requested"
                    });
                }
                break;
        }

        await _context.SaveChangesAsync();

        // Update score & qualification on any linked leads
        if (prospect != null)
        {
            var linkedLeads = await _context.Leads.Where(l => l.ProspectId == prospect.ProspectId || l.Email == prospect.Email).ToListAsync();
            foreach (var lead in linkedLeads)
            {
                var scoringEventType = eventUpper switch
                {
                    "OPEN" => "EMAIL_OPEN",
                    "CLICK" => "EMAIL_CLICK",
                    "REPLY" => "EMAIL_REPLY",
                    "BOUNCE" => "EMAIL_BOUNCE",
                    _ => null
                };

                if (scoringEventType != null)
                {
                    await _scoringService.ApplyScoreEventAsync(lead.LeadId, scoringEventType, $"Engagement event received: {eventUpper}");
                    await _qualificationService.EvaluateQualificationAsync(lead.LeadId);
                }
            }
        }
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
    }
}