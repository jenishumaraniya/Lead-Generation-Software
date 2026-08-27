using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/email")]
public class EmailController : ControllerBase
{
    private readonly EmailService _emailService;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public EmailController(EmailService emailService, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _emailService = emailService;
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Trigger email sending for a campaign recipient.
    /// POST /api/email/send
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailSendDto dto)
    {
        try
        {
            var result = await _emailService.SendEmailAsync(dto.CampaignRecipientId, dto.FromEmail);
            return Ok(new
            {
                messageId = result.EmailMessageId,
                sentAt = result.SentAt,
                status = result.Status
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Diagnostic endpoint — send a quick test email via Mailtrap Sending API.
    /// POST /api/email/test   Body (JSON string): "recipient@example.com"
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestEmail([FromBody] string toEmail)
    {
        try
        {
            var apiKey = _config["Mailtrap:ApiKey"];
            var fromEmail = _config["Email:From"] ?? "noreply@apigod.in";
            var fromName = _config["Mailtrap:FromName"] ?? "CRM System";

            if (string.IsNullOrEmpty(apiKey) || apiKey.StartsWith("YOUR_"))
            {
                return BadRequest(new
                {
                    success = false,
                    error = "Mailtrap API key is not configured. Add your key to appsettings.json under Mailtrap:ApiKey."
                });
            }

            var payload = new
            {
                from = new { email = fromEmail, name = fromName },
                to = new[] { new { email = toEmail } },
                subject = "Test Email from CRM System",
                html = $@"
                    <h2>Test Successful</h2>
                    <p>Your Mailtrap Sending API integration is working correctly.</p>
                    <p><strong>Sent at:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                    <p><strong>To:</strong> {toEmail}</p>
                    <p><strong>From:</strong> {fromEmail}</p>
                ",
                text = $"Test Successful. Sent at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC. To: {toEmail}"
            };

            var json = JsonSerializer.Serialize(payload);

            using var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://send.api.mailtrap.io/api/send");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return Ok(new
                {
                    success = true,
                    message = "Email sent successfully via Mailtrap.",
                    to = toEmail,
                    from = fromEmail,
                    mailtrapResponse = responseBody
                });
            }
            else
            {
                // Parse Mailtrap error body for clearer message
                string errorDetail = responseBody;
                try
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.TryGetProperty("errors", out var errors))
                        errorDetail = string.Join(", ", errors.EnumerateArray().Select(e => e.GetString() ?? "unknown"));
                }
                catch { }

                return StatusCode((int)response.StatusCode, new
                {
                    success = false,
                    statusCode = (int)response.StatusCode,
                    error = errorDetail,
                    hint = "Verify (1) your API key is correct, (2) the 'from' domain is verified in Mailtrap > Sending > Domains."
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Receives Mailtrap webhook events (open, click, bounce, unsubscribe).
    /// POST /api/email/webhook
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] EmailEventWebhookDto dto)
    {
        try
        {
            await _emailService.ProcessEmailEventAsync(dto);
            return Ok();
        }
        catch (Exception)
        {
            // Always return 200 to prevent Mailtrap from retrying endlessly
            return Ok();
        }
    }
}