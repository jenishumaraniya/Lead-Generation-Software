namespace CrmLeadTool.Api.DTOs;

public class CampaignDto
{
    public int CampaignId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ScheduleStartDate { get; set; }
    public DateTime? ScheduleEndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<SequenceStepDto> Steps { get; set; } = new();
}

public class SequenceStepDto
{
    public int SequenceStepId { get; set; }
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int DelayDays { get; set; }
    public int DelayHours { get; set; }
    public bool IsActive { get; set; }
}