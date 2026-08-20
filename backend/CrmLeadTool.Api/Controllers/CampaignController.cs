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
    public async Task<IActionResult> CreateCampaign(CreateCampaignDto dto)
    {
        var campaign = await _campaignService.CreateCampaignAsync(dto);
        return CreatedAtAction(nameof(GetCampaign), new { id = campaign.CampaignId }, campaign);
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
            return NotFound();
        return Ok(campaign);
    }

    [HttpPost("{id}/enroll")]
    public async Task<IActionResult> EnrollProspect(int id, [FromBody] EnrollProspectDto dto)
    {
        try
        {
            var recipient = await _campaignService.EnrollProspectAsync(id, dto.ProspectId);
            return Ok(recipient);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        try
        {
            await _campaignService.UpdateCampaignStatusAsync(id, status);
            return Ok(new { message = "Status updated." });
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}