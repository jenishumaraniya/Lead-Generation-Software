using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Services;

public class ProspectService
{
    private readonly AppDbContext _context;

    public ProspectService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Prospect> CreateProspectAsync(CreateProspectDto dto)
    {
        // Check duplicate by email
        var existing = await _context.Prospects
            .FirstOrDefaultAsync(p => p.Email == dto.Email);
        if (existing != null)
            throw new InvalidOperationException("Prospect with this email already exists.");

        // Find or create company
        Company? company = null;
        if (!string.IsNullOrEmpty(dto.CompanyName))
        {
            company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Name == dto.CompanyName || c.Domain == dto.CompanyDomain);
            if (company == null)
            {
                company = new Company
                {
                    Name = dto.CompanyName,
                    Domain = dto.CompanyDomain,
                    Industry = dto.Industry,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();
            }
        }

        // Find visitor by AnonymousId if provided
        int? visitorId = null;
        if (!string.IsNullOrEmpty(dto.VisitorId))
        {
            var visitor = await _context.Visitors
                .FirstOrDefaultAsync(v => v.AnonymousId == dto.VisitorId);
            visitorId = visitor?.VisitorId;
        }

        var prospect = new Prospect
        {
            CompanyId = company?.CompanyId,
            VisitorId = visitorId,
            Email = dto.Email,
            Name = dto.Name,
            JobTitle = dto.JobTitle,
            Phone = dto.Phone,
            LinkedInUrl = dto.LinkedInUrl,
            Source = dto.Source ?? "MANUAL",
            Status = "NEW",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Prospects.Add(prospect);
        await _context.SaveChangesAsync();
        return prospect;
    }

    public async Task<List<Prospect>> GetAllProspectsAsync()
    {
        return await _context.Prospects
            .Include(p => p.Company)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Prospect?> GetProspectAsync(int id)
    {
        return await _context.Prospects
            .Include(p => p.Company)
            .Include(p => p.CampaignRecipients)
                .ThenInclude(cr => cr.Campaign)
            .FirstOrDefaultAsync(p => p.ProspectId == id);
    }

    public async Task<Prospect> UpdateProspectAsync(int id, UpdateProspectDto dto)
    {
        var prospect = await _context.Prospects.FindAsync(id);
        if (prospect == null)
            throw new ArgumentException("Prospect not found.");

        prospect.Name = dto.Name ?? prospect.Name;
        prospect.JobTitle = dto.JobTitle ?? prospect.JobTitle;
        prospect.Phone = dto.Phone ?? prospect.Phone;
        prospect.LinkedInUrl = dto.LinkedInUrl ?? prospect.LinkedInUrl;
        prospect.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return prospect;
    }
}