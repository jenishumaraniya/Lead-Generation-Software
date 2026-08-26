using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/campaigns")]
public class CampaignController : ControllerBase
{
    private readonly CampaignService _campaignService;

    public CampaignController(CampaignService campaignService)
    {
        _campaignService = campaignService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignDto dto)
    {
        try
        {
            var campaign = await _campaignService.CreateCampaignAsync(dto);
            return CreatedAtAction(nameof(GetCampaign), new { id = campaign.CampaignId }, new
            {
                campaign.CampaignId,
                campaign.Name,
                campaign.Description,
                campaign.Status,
                campaign.ScheduleStartDate,
                campaign.ScheduleEndDate,
                campaign.CreatedAt,
                campaign.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCampaigns()
    {
        var campaigns = await _campaignService.GetAllCampaignsAsync();
        return Ok(campaigns);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCampaign(int id)
    {
        var campaign = await _campaignService.GetCampaignAsync(id);
        if (campaign == null)
            return NotFound(new { error = $"Campaign with ID {id} not found." });
        return Ok(campaign);
    }

    [HttpGet("{id}/recipients")]
    public async Task<IActionResult> GetCampaignRecipients(int id)
    {
        var recipients = await _campaignService.GetCampaignRecipientsAsync(id);
        return Ok(recipients);
    }

    [HttpPost("{id}/enroll")]
    public async Task<IActionResult> EnrollProspect(int id, [FromBody] EnrollProspectDto dto)
    {
        try
        {
            var recipient = await _campaignService.EnrollProspectAsync(id, dto.ProspectId);
            return Ok(new
            {
                recipient.CampaignRecipientId,
                recipient.CampaignId,
                recipient.ProspectId,
                recipient.Status,
                recipient.CurrentStep,
                recipient.EnrolledAt,
                recipient.LastActivityAt,
                recipient.CompletedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/pause")]
    public async Task<IActionResult> PauseCampaign(int id)
    {
        try
        {
            var campaign = await _campaignService.PauseCampaignAsync(id);
            return Ok(new { success = true, campaign.CampaignId, campaign.Status });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/resume")]
    public async Task<IActionResult> ResumeCampaign(int id)
    {
        try
        {
            var campaign = await _campaignService.ResumeCampaignAsync(id);
            return Ok(new { success = true, campaign.CampaignId, campaign.Status });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut("recipients/{recipientId}/status")]
    public async Task<IActionResult> UpdateRecipientStatus(int recipientId, [FromBody] UpdateRecipientStatusDto dto)
    {
        try
        {
            var recipient = await _campaignService.UpdateRecipientStatusAsync(recipientId, dto.Status);
            return Ok(recipient);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

public class EnrollProspectDto
{
    public int ProspectId { get; set; }
}

public class UpdateRecipientStatusDto
{
    public string Status { get; set; } = "PAUSED";
}