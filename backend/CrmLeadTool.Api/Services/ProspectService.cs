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
        var existing = await _context.Prospects
            .FirstOrDefaultAsync(p => p.Email == dto.Email);
        if (existing != null)
            throw new InvalidOperationException("Prospect with this email already exists.");

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

        Guid? publicId = null;
        if (!string.IsNullOrEmpty(dto.VisitorId))
        {
            var visitor = await _context.Visitors
                .FirstOrDefaultAsync(v => v.AnonymousId == dto.VisitorId);
            if (visitor != null)
                publicId = visitor.PublicId;
        }

        var prospect = new Prospect
        {
            CompanyId = company?.CompanyId,
            PublicId = publicId,
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

    public async Task<List<object>> GetAllProspectsAsync()
    {
        return await _context.Prospects
            .Include(p => p.Company)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.ProspectId,
                p.CompanyId,
                p.PublicId,
                p.Email,
                p.Name,
                p.JobTitle,
                p.Phone,
                p.LinkedInUrl,
                p.Source,
                p.Status,
                p.Score,
                p.Qualification,
                p.CreatedAt,
                p.UpdatedAt,
                Company = p.Company != null ? new
                {
                    p.Company.CompanyId,
                    p.Company.Name,
                    p.Company.Domain,
                    p.Company.Industry,
                    p.Company.Size,
                    p.Company.Location,
                    p.Company.Description,
                    p.Company.CreatedAt
                } : null
            })
            .ToListAsync<object>();
    }

    public async Task<object?> GetProspectAsync(int id)
    {
        return await _context.Prospects
            .Include(p => p.Company)
            .Where(p => p.ProspectId == id)
            .Select(p => new
            {
                p.ProspectId,
                p.CompanyId,
                p.PublicId,
                p.Email,
                p.Name,
                p.JobTitle,
                p.Phone,
                p.LinkedInUrl,
                p.Source,
                p.Status,
                p.Score,
                p.Qualification,
                p.CreatedAt,
                p.UpdatedAt,
                Company = p.Company != null ? new
                {
                    p.Company.CompanyId,
                    p.Company.Name,
                    p.Company.Domain,
                    p.Company.Industry,
                    p.Company.Size,
                    p.Company.Location,
                    p.Company.Description,
                    p.Company.CreatedAt
                } : null
            })
            .FirstOrDefaultAsync();
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

    public async Task<object> ImportCsvProspectsAsync(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new ArgumentException("CSV file is empty.");
        }

        var headers = headerLine.Split(',').Select(h => h.Trim().ToLowerInvariant().Replace(" ", "").Replace("_", "")).ToArray();
        int nameIdx = Array.FindIndex(headers, h => h.Contains("name") && !h.Contains("company"));
        int emailIdx = Array.FindIndex(headers, h => h.Contains("email"));
        int companyIdx = Array.FindIndex(headers, h => h.Contains("company"));
        int titleIdx = Array.FindIndex(headers, h => h.Contains("title") || h.Contains("job") || h.Contains("role"));
        int industryIdx = Array.FindIndex(headers, h => h.Contains("industry"));
        int phoneIdx = Array.FindIndex(headers, h => h.Contains("phone"));
        int domainIdx = Array.FindIndex(headers, h => h.Contains("domain") || h.Contains("website"));
        int countryIdx = Array.FindIndex(headers, h => h.Contains("country") || h.Contains("location"));

        if (emailIdx == -1)
        {
            throw new ArgumentException("CSV must contain an 'Email' column.");
        }

        int addedCount = 0;
        int updatedCount = 0;
        var importedProspects = new List<Prospect>();

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var values = line.Split(',').Select(v => v.Trim().Trim('"')).ToArray();

            string email = emailIdx < values.Length ? values[emailIdx].Trim().ToLowerInvariant() : "";
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) continue;

            string name = nameIdx >= 0 && nameIdx < values.Length ? values[nameIdx] : "";
            string companyName = companyIdx >= 0 && companyIdx < values.Length ? values[companyIdx] : "";
            string jobTitle = titleIdx >= 0 && titleIdx < values.Length ? values[titleIdx] : "";
            string industry = industryIdx >= 0 && industryIdx < values.Length ? values[industryIdx] : "";
            string phone = phoneIdx >= 0 && phoneIdx < values.Length ? values[phoneIdx] : "";
            string domain = domainIdx >= 0 && domainIdx < values.Length ? values[domainIdx] : "";
            string location = countryIdx >= 0 && countryIdx < values.Length ? values[countryIdx] : "";

            Company? company = null;
            if (!string.IsNullOrWhiteSpace(companyName))
            {
                company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == companyName.ToLower() || (!string.IsNullOrEmpty(domain) && c.Domain == domain));

                if (company == null)
                {
                    company = new Company
                    {
                        Name = companyName,
                        Domain = domain,
                        Industry = industry,
                        Location = location,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();
                }
            }

            var prospect = await _context.Prospects.FirstOrDefaultAsync(p => p.Email.ToLower() == email);
            if (prospect == null)
            {
                prospect = new Prospect
                {
                    Email = email,
                    Name = !string.IsNullOrWhiteSpace(name) ? name : email.Split('@')[0],
                    JobTitle = jobTitle,
                    Phone = phone,
                    CompanyId = company?.CompanyId,
                    Source = "CSV_IMPORT",
                    Status = "NEW",
                    Score = 10,
                    Qualification = "COLD",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Prospects.Add(prospect);
                addedCount++;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(name)) prospect.Name = name;
                if (!string.IsNullOrWhiteSpace(jobTitle)) prospect.JobTitle = jobTitle;
                if (!string.IsNullOrWhiteSpace(phone)) prospect.Phone = phone;
                if (company != null) prospect.CompanyId = company.CompanyId;
                prospect.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
            }

            await _context.SaveChangesAsync();
            importedProspects.Add(prospect);
        }

        return new
        {
            totalProcessed = addedCount + updatedCount,
            added = addedCount,
            updated = updatedCount,
            prospects = importedProspects.Select(p => new { p.ProspectId, p.Name, p.Email, p.JobTitle, p.Status, p.Score })
        };
    }

    public async Task DeleteProspectAsync(int id)
    {
        var prospect = await _context.Prospects.FindAsync(id);
        if (prospect == null)
            throw new ArgumentException("Prospect not found.");

        prospect.Status = "INACTIVE";
        prospect.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}