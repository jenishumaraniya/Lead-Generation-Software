using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmLeadTool.Api.Controllers;

[ApiController]
[Route("api/visitor")]
public class VisitorController : ControllerBase
{
    private readonly VisitorService _visitorService;

    public VisitorController(VisitorService visitorService)
    {
        _visitorService = visitorService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(CreateVisitorDto dto)
    {
        var visitor = await _visitorService.CreateVisitor(dto.ConsentStatus ?? "UNKNOWN");
        return Ok(new
        {
            visitorId = visitor.VisitorId,
            anonymousId = visitor.AnonymousId
        });
    }
}