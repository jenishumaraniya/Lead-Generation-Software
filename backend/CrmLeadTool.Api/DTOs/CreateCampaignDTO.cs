using System.ComponentModel.DataAnnotations;

namespace CrmLeadTool.Api.DTOs;

public class CreateCampaignDto
{
    [Required(ErrorMessage = "Campaign Name is mandatory.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Status { get; set; }

    [Required(ErrorMessage = "Scheduled Start Date is mandatory.")]
    public DateTime? ScheduleStartDate { get; set; }

    [Required(ErrorMessage = "Scheduled End Date is mandatory.")]
    public DateTime? ScheduleEndDate { get; set; }

    public bool SendImmediately { get; set; } = false;

    public List<CreateSequenceStepDto> Steps { get; set; } = new();

    public List<int>? ProspectIds { get; set; } 
}

public class CreateSequenceStepDto
{
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int DelayDays { get; set; }
    public int DelayHours { get; set; }
}