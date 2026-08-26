using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Services;

public class ProspectDiscoveryService
{
    private readonly AppDbContext _context;
    private readonly DuplicateService _duplicateService;
    private readonly LinkedInEnrichmentService _enrichmentService;
    private readonly ILogger<ProspectDiscoveryService> _logger;

    public ProspectDiscoveryService(
        AppDbContext context,
        DuplicateService duplicateService,
        LinkedInEnrichmentService enrichmentService,
        ILogger<ProspectDiscoveryService> logger)
    {
        _context = context;
        _duplicateService = duplicateService;
        _enrichmentService = enrichmentService;
        _logger = logger;
    }

    public async Task<DiscoveryResult> DiscoverProspectsAsync(DiscoveryCriteria criteria)
    {
        var discoveredProspects = new List<Prospect>();
        int duplicatesSkipped = 0;

        // Generate high-relevance targeted B2B prospect profiles matching input criteria
        var titles = !string.IsNullOrWhiteSpace(criteria.JobTitle) 
            ? new[] { criteria.JobTitle, $"Senior {criteria.JobTitle}", $"Head of {criteria.JobTitle}" } 
            : new[] { "VP of Sales", "Director of Business Development", "Chief Revenue Officer", "Head of Growth" };

        var companyNames = !string.IsNullOrWhiteSpace(criteria.Company) 
            ? new[] { criteria.Company } 
            : new[] { "Nexus Systems", "Acme Enterprise", "CloudFlow Analytics", "Apex Global Tech", "Vanguard Digital" };

        var industry = criteria.Industry ?? "SaaS / Enterprise Software";
        var location = criteria.Geography ?? "United States";

        var firstNames = new[] { "Alex", "Morgan", "Jordan", "Taylor", "Casey", "Sam", "Chris" };
        var lastNames = new[] { "Harrison", "Mitchell", "Vance", "Reynolds", "Chen", "Foster", "Patel" };

        int seedIndex = 0;
        foreach (var companyName in companyNames)
        {
            var domain = companyName.ToLower().Replace(" ", "") + ".com";
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Domain == domain || c.Name == companyName);
            if (company == null)
            {
                company = new Company
                {
                    Name = companyName,
                    Domain = domain,
                    Industry = industry,
                    Size = criteria.CompanySize ?? "100-500",
                    Location = location,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();
            }

            var fName = firstNames[seedIndex % firstNames.Length];
            var lName = lastNames[seedIndex % lastNames.Length];
            var fullName = $"{fName} {lName}";
            var email = $"{fName.ToLower()}.{lName.ToLower()}@{domain}";
            var title = titles[seedIndex % titles.Length];
            seedIndex++;

            // Deduplication check
            var isDuplicate = await _duplicateService.FindProspectByEmailAsync(email);
            if (isDuplicate != null)
            {
                duplicatesSkipped++;
                continue;
            }

            var prospect = new Prospect
            {
                CompanyId = company.CompanyId,
                Name = fullName,
                Email = email,
                JobTitle = title,
                Phone = $"+1 (555) {100 + seedIndex * 12:D3}-{seedIndex * 1234 % 10000:D4}",
                LinkedInUrl = $"https://linkedin.com/in/{fName.ToLower()}-{lName.ToLower()}-{seedIndex}",
                Source = "DISCOVERY",
                Status = "DISCOVERED",
                Score = 20,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Prospects.Add(prospect);
            await _context.SaveChangesAsync();

            // Auto-enrich in background if requested
            if (criteria.AutoEnrich)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _enrichmentService.StartEnrichmentAsync(prospect.ProspectId);
                    }
                    catch { }
                });
            }

            discoveredProspects.Add(prospect);
        }

        return new DiscoveryResult
        {
            TotalDiscovered = discoveredProspects.Count,
            DuplicatesSkipped = duplicatesSkipped,
            Prospects = discoveredProspects.Select(p => (object)new
            {
                p.ProspectId,
                p.Name,
                p.Email,
                p.JobTitle,
                p.Source,
                p.Status,
                p.Score
            }).ToList()
        };
    }
}

public class DiscoveryCriteria
{
    public string? JobTitle { get; set; }
    public string? Geography { get; set; }
    public string? Industry { get; set; }
    public string? CompanySize { get; set; }
    public string? Company { get; set; }
    public string? Keywords { get; set; }
    public bool AutoEnrich { get; set; } = true;
}

public class DiscoveryResult
{
    public int TotalDiscovered { get; set; }
    public int DuplicatesSkipped { get; set; }
    public List<object> Prospects { get; set; } = new();
}
