using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CrmLeadTool.Api.Services;

public class DuplicateLeadException : Exception
{
    public List<int> DuplicateLeadIds { get; }

    public DuplicateLeadException(string message, List<int> duplicateLeadIds)
        : base(message)
    {
        DuplicateLeadIds = duplicateLeadIds;
    }
}

public class LeadService
{
    private readonly AppDbContext _context;
    private readonly DuplicateService _duplicateService;

    public LeadService(AppDbContext context, DuplicateService duplicateService)
    {
        _context = context;
        _duplicateService = duplicateService;
    }

    public async Task<Lead> CreateLeadFromFormAsync(LeadSubmitDto dto)
    {
        // 1. Check for duplicates
        var duplicates = await _duplicateService.FindAllDuplicatesAsync(dto);
        if (duplicates.Any())
        {
            throw new DuplicateLeadException(
                "Lead already exists.",
                duplicates.Select(d => d.LeadId).ToList()
            );
        }

        // 2. Find Visitor by AnonymousId
        int? visitorId = null;
        if (!string.IsNullOrEmpty(dto.VisitorId))
        {
            var visitor = await _context.Visitors
                .FirstOrDefaultAsync(v => v.AnonymousId == dto.VisitorId);
            if (visitor != null)
                visitorId = visitor.VisitorId;
        }

        // 3. Find Prospect by Email
        int? prospectId = null;
        if (!string.IsNullOrEmpty(dto.ProspectEmail))
        {
            var prospect = await _duplicateService.FindProspectByEmailAsync(dto.ProspectEmail);
            if (prospect != null)
                prospectId = prospect.ProspectId;
        }

        // 4. Create Lead
        var lead = new Lead
        {
            VisitorId = visitorId,
            ProspectId = prospectId,
            CompanyName = dto.CompanyName,
            FullName = dto.FullName,
            Email = dto.Email,
            JobTitle = dto.JobTitle,
            Domain = dto.Domain,
            Industry = dto.Industry,
            Country = dto.Country,
            Phone = dto.Phone,
            Quantity = dto.Quantity,
            Timeline = dto.Timeline,
            BusinessRequirement = dto.BusinessRequirement,
            Source = dto.Source ?? "WEBSITE_FORM",
            Status = "NEW",
            Score = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        lead.SetProductIdList(dto.Products);

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        return lead;
    }

    public async Task<List<Lead>> GetAllLeadsAsync()
    {
        // ✅ Remove Includes - just get the leads
        return await _context.Leads
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<Lead?> GetLeadByIdAsync(int id)
    {
        // ✅ Simple query without Includes
        return await _context.Leads
            .FirstOrDefaultAsync(l => l.LeadId == id);
    }

    public async Task<List<Lead>> SearchLeadsAsync(LeadSearchDto dto)
    {
        var query = _context.Leads.AsQueryable();

        if (!string.IsNullOrEmpty(dto.Email))
            query = query.Where(l => l.Email.Contains(dto.Email));

        if (!string.IsNullOrEmpty(dto.CompanyName))
            query = query.Where(l => l.CompanyName.Contains(dto.CompanyName));

        if (!string.IsNullOrEmpty(dto.FullName))
            query = query.Where(l => l.FullName.Contains(dto.FullName));

        if (!string.IsNullOrEmpty(dto.Status))
            query = query.Where(l => l.Status == dto.Status);

        if (!string.IsNullOrEmpty(dto.Qualification))
            query = query.Where(l => l.Qualification == dto.Qualification);

        if (dto.MinScore.HasValue)
            query = query.Where(l => l.Score >= dto.MinScore);

        if (dto.MaxScore.HasValue)
            query = query.Where(l => l.Score <= dto.MaxScore);

        if (dto.FromDate.HasValue)
            query = query.Where(l => l.CreatedAt >= dto.FromDate);

        if (dto.ToDate.HasValue)
            query = query.Where(l => l.CreatedAt <= dto.ToDate);

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<Lead> UpdateLeadAsync(int id, LeadUpdateDto dto)
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null)
            throw new ArgumentException("Lead not found.");

        var oldStatus = lead.Status;

        if (!string.IsNullOrEmpty(dto.Status))
            lead.Status = dto.Status;

        if (!string.IsNullOrEmpty(dto.Qualification))
            lead.Qualification = dto.Qualification;

        if (dto.Score.HasValue)
            lead.Score = dto.Score;

        lead.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return lead;
    }

    public async Task AddLeadNoteAsync(int leadId, string note, string? createdBy = null)
    {
        var leadNote = new LeadNote
        {
            LeadId = leadId,
            NoteText = note,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy ?? "SYSTEM"
        };
        _context.LeadNotes.Add(leadNote);
        await _context.SaveChangesAsync();
    }

    public async Task<Lead> ConvertProspectToLeadAsync(int prospectId, LeadSubmitDto dto)
    {
        var prospect = await _context.Prospects
            .FirstOrDefaultAsync(p => p.ProspectId == prospectId);
        if (prospect == null)
            throw new ArgumentException("Prospect not found.");

        var existingLead = await _context.Leads
            .FirstOrDefaultAsync(l => l.ProspectId == prospectId);
        if (existingLead != null)
            throw new InvalidOperationException("Lead already exists for this prospect.");

        var lead = new Lead
        {
            VisitorId = prospect.VisitorId,
            ProspectId = prospect.ProspectId,
            CompanyName = dto.CompanyName,
            FullName = prospect.Name,
            Email = prospect.Email,
            JobTitle = prospect.JobTitle ?? dto.JobTitle,
            Phone = prospect.Phone ?? dto.Phone,
            Industry = dto.Industry,
            Source = "PROSPECT_CONVERSION",
            Status = "NEW",
            Score = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        lead.SetProductIdList(dto.Products ?? Array.Empty<int>());

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        prospect.Status = "CONVERTED";
        await _context.SaveChangesAsync();

        return lead;
    }
}