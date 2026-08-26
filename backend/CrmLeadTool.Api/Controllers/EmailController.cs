using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/email")]
public class EmailController : ControllerBase
{
    private readonly EmailService _emailService;
    private readonly IConfiguration _config;

    public EmailController(EmailService emailService, IConfiguration config)
    {
        _emailService = emailService;
        _config = config;
    }

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

[HttpPost("test")]
public async Task<IActionResult> TestEmail([FromBody] string toEmail)
{
    try
    {
        var apiKey = _config["Mailtrap:ApiKey"];
        var fromEmail = _config["Email:From"];
        var fromName = _config["Mailtrap:FromName"] ?? "CRM System";

        // Validate API key
        if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_MAILTRAP_API_KEY_HERE")
        {
            return BadRequest(new { 
                success = false, 
                error = "Mailtrap API key is not configured. Please add your API key to appsettings.json" 
            });
        }

        // Validate from email
        if (string.IsNullOrEmpty(fromEmail) || fromEmail == "noreply@yourdomain.com")
        {
            return BadRequest(new { 
                success = false, 
                error = "From email is not configured. Please add a verified domain email to appsettings.json" 
            });
        }

        using var httpClient = new HttpClient();
        var payload = new
        {
            from = new { email = fromEmail, name = fromName },
            to = new[] { new { email = toEmail } },
            subject = "✅ Test Email from CRM",
            html = $@"
                <h1>✅ Test Successful!</h1>
                <p>Your Mailtrap integration is working correctly.</p>
                <p>Sent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                <hr>
                <p><strong>To:</strong> {toEmail}</p>
                <p><strong>From:</strong> {fromEmail}</p>
                <p><strong>API Key:</strong> {apiKey.Substring(0, 8)}...{apiKey.Substring(apiKey.Length - 4)}</p>
            ",
            text = $"Test Successful! Sent at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var response = await httpClient.PostAsync(
            "https://send.api.mailtrap.io/api/send",
            content
        );

        var responseBody = await response.Content.ReadAsStringAsync();

        // Log the full response for debugging
        Console.WriteLine($"Mailtrap Response Status: {response.StatusCode}");
        Console.WriteLine($"Mailtrap Response Body: {responseBody}");

        if (response.IsSuccessStatusCode)
        {
            return Ok(new
            {
                success = true,
                message = "Email sent successfully!",
                to = toEmail,
                from = fromEmail,
                response = responseBody
            });
        }
        else
        {
            // Try to parse the error
            string errorMessage = responseBody;
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("errors", out var errors))
                {
                    var errorList = new List<string>();
                    foreach (var error in errors.EnumerateArray())
                    {
                        errorList.Add(error.GetString() ?? "Unknown error");
                    }
                    errorMessage = string.Join(", ", errorList);
                }
            }
            catch { }

            return StatusCode((int)response.StatusCode, new
            {
                success = false,
                statusCode = (int)response.StatusCode,
                error = errorMessage,
                fullResponse = responseBody
            });
        }
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { 
            success = false, 
            error = ex.Message,
            stackTrace = ex.StackTrace 
        });
    }
}
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
            return Ok();
        }
    }
}