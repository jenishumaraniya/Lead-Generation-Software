namespace CrmLeadTool.Api.DTOs;

public class CreateCampaignDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Status { get; set; }
    public DateTime? ScheduleStartDate { get; set; }
    public DateTime? ScheduleEndDate { get; set; }
    public List<CreateSequenceStepDto> Steps { get; set; } = new();
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