using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Services;

public class LeadService
{
    private readonly AppDbContext _context;
    private readonly DuplicateService _duplicateService;
    private readonly ScoringService _scoringService;
    private readonly QualificationService _qualificationService;
    private readonly ILogger<LeadService> _logger;

    public LeadService(
        AppDbContext context,
        DuplicateService duplicateService,
        ScoringService scoringService,
        QualificationService qualificationService,
        ILogger<LeadService> logger)
    {
        _context = context;
        _duplicateService = duplicateService;
        _scoringService = scoringService;
        _qualificationService = qualificationService;
        _logger = logger;
    }

    public async Task<Lead> CreateLeadFromFormAsync(LeadSubmitDto dto)
    {
        // 1. Multi-key deduplication
        var existing = await _duplicateService.FindDuplicateLeadAsync(dto.Email, dto.Phone, dto.FullName, dto.CompanyName);
        if (existing != null)
        {
            // Update existing lead context and append requirement
            existing.BusinessRequirement = $"{existing.BusinessRequirement} | [Update {DateTime.UtcNow:g}]: {dto.BusinessRequirement}";
            if (!string.IsNullOrEmpty(dto.Timeline)) existing.Timeline = dto.Timeline;
            if (dto.Quantity.HasValue) existing.Quantity = dto.Quantity;
            
            var effectivePids = dto.GetEffectiveProductIds();
            if (effectivePids.Length > 0)
            {
                existing.SetProductIdList(effectivePids);
                var existingCategories = await _context.Products
                    .Where(p => effectivePids.Contains(p.ProductId) && p.CategoryId.HasValue)
                    .Include(p => p.Category)
                    .Select(p => new { p.CategoryId, CategoryName = p.Category != null ? p.Category.CategoryName : "Unknown" })
                    .Distinct()
                    .ToListAsync();

                if (existingCategories.Count > 1)
                {
                    existing.IsMultiCategory = true;
                }
                else if (existingCategories.Count == 1)
                {
                    existing.IsMultiCategory = false;
                    if (!existing.AssignedTo.HasValue)
                    {
                        var rep = await _context.Users
                            .FirstOrDefaultAsync(u => u.CategoryId == existingCategories[0].CategoryId!.Value && u.Role == "SALES_REP" && u.IsActive);
                        if (rep != null) existing.AssignedTo = rep.UserId;
                    }
                }
            }

            existing.UpdatedAt = DateTime.UtcNow;

            await _scoringService.ApplyScoreEventAsync(existing.LeadId, "REPEAT_VISIT", "Returning lead submitted additional inquiry", 15);
            await _qualificationService.EvaluateQualificationAsync(existing.LeadId);
            await _context.SaveChangesAsync();
            return existing;
        }

        // 2. Link Visitor and Prospect if available
        int? visitorId = null;
        if (!string.IsNullOrEmpty(dto.VisitorId))
        {
            var visitor = await _context.Visitors.FirstOrDefaultAsync(v => v.AnonymousId == dto.VisitorId);
            if (visitor != null)
            {
                visitorId = visitor.VisitorId;
                visitor.LastSeenAt = DateTime.UtcNow;
            }
        }

        int? prospectId = null;
        if (!string.IsNullOrEmpty(dto.ProspectEmail) || !string.IsNullOrEmpty(dto.Email))
        {
            var searchEmail = string.IsNullOrEmpty(dto.ProspectEmail) ? dto.Email : dto.ProspectEmail;
            var prospect = await _duplicateService.FindProspectByEmailAsync(searchEmail);
            if (prospect != null) prospectId = prospect.ProspectId;
        }

        // 3. Category & Salesperson Routing Analysis
        var productIds = dto.GetEffectiveProductIds();
        var categories = await _context.Products
            .Where(p => productIds.Contains(p.ProductId) && p.CategoryId.HasValue)
            .Include(p => p.Category)
            .Select(p => new { p.CategoryId, CategoryName = p.Category != null ? p.Category.CategoryName : "Unknown" })
            .Distinct()
            .ToListAsync();

        bool isMultiCategory = false;
        int? assignedTo = null;
        string? initialActivityNote = null;

        if (categories.Count > 1 || categories.Count == 0)
        {
            // Requirement: Person wants products from more than 1 category (or generic unassigned inquiry) => Admin decides which sales person that lead should go
            isMultiCategory = true;
            assignedTo = null;
            var catNames = categories.Count > 0 ? string.Join(", ", categories.Select(c => c.CategoryName)) : "General Inquiry";
            initialActivityNote = categories.Count > 1
                ? $"Inquiry includes products across {categories.Count} categories ({catNames}). Flagged for Admin manual assignment."
                : "Inbound commercial inquiry received. Awaiting Admin assignment.";
        }
        else if (categories.Count == 1)
        {
            // Requirement: Simple lead (1 category) => Auto-assign to the salesperson assigned to that category
            isMultiCategory = false;
            int categoryId = categories[0].CategoryId!.Value;
            var categoryName = categories[0].CategoryName;

            var salesRep = await _context.Users
                .FirstOrDefaultAsync(u => u.CategoryId == categoryId && u.Role == "SALES_REP" && u.IsActive);

            if (salesRep != null)
            {
                assignedTo = salesRep.UserId;
                initialActivityNote = $"Lead automatically routed to sales representative '{salesRep.FullName}' for category '{categoryName}'.";
            }
            else
            {
                initialActivityNote = $"Lead inquiry for category '{categoryName}'. No active salesperson currently assigned to this category.";
            }
        }

        // 4. Create lead record
        var lead = new Lead
        {
            VisitorId = visitorId,
            ProspectId = prospectId,
            AssignedTo = assignedTo,
            IsMultiCategory = isMultiCategory,
            CompanyName = dto.CompanyName ?? string.Empty,
            FullName = dto.FullName ?? string.Empty,
            Email = dto.Email ?? string.Empty,
            JobTitle = dto.JobTitle ?? string.Empty,
            Domain = dto.Domain ?? string.Empty,
            Industry = dto.Industry ?? string.Empty,
            Country = dto.Country ?? string.Empty,
            Phone = dto.Phone ?? string.Empty,
            Quantity = dto.Quantity,
            Timeline = dto.Timeline ?? string.Empty,
            BusinessRequirement = dto.BusinessRequirement ?? string.Empty,
            Source = dto.Source ?? "WEBSITE_FORM",
            Status = "NEW",
            Score = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        lead.SetProductIdList(productIds);

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        // 5. Initial Status History & Activity Log
        _context.LeadStatusHistories.Add(new LeadStatusHistory
        {
            LeadId = lead.LeadId,
            OldStatus = null,
            NewStatus = "NEW",
            ChangedBy = "System",
            Reason = "Inbound lead inquiry captured",
            ChangedAt = DateTime.UtcNow
        });

        if (!string.IsNullOrEmpty(initialActivityNote))
        {
            _context.LeadActivities.Add(new LeadActivity
            {
                LeadId = lead.LeadId,
                ActivityType = isMultiCategory ? "MULTI_CATEGORY_INQUIRY" : (assignedTo.HasValue ? "AUTO_ASSIGNMENT" : "INQUIRY_RECEIVED"),
                Description = initialActivityNote,
                CreatedBy = "System Routing",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        // 6. Initial scoring & qualification evaluation
        await _scoringService.ApplyScoreEventAsync(lead.LeadId, "FORM_SUBMIT", "Inbound form submitted with commercial requirement");

        var titleLower = (dto.JobTitle ?? "").ToLower();
        if (titleLower.Contains("vp") || titleLower.Contains("director") || titleLower.Contains("head") || titleLower.Contains("chief") || titleLower.Contains("manager"))
        {
            await _scoringService.ApplyScoreEventAsync(lead.LeadId, "ROLE_MATCH", $"Target decision maker role identified: {dto.JobTitle}");
        }

        await _qualificationService.EvaluateQualificationAsync(lead.LeadId);

        return lead;
    }

    public async Task<List<object>> GetAllLeadsAsync(int? assignedTo = null)
    {
        var leads = await _context.Leads
            .Include(l => l.Visitor)
            .Include(l => l.Prospect)
            .Include(l => l.AssignedUser)
                .ThenInclude(u => u!.Category)
            .Include(l => l.ScoreHistories)
            .Include(l => l.StatusHistories)
            .Include(l => l.LeadNotes)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        // Dynamic category evaluation & auto-assignment for leads
        var activeReps = await _context.Users
            .Where(u => u.Role == "SALES_REP" && u.IsActive && u.CategoryId.HasValue)
            .Include(u => u.Category)
            .ToListAsync();

        var allProds = await _context.Products.Where(p => p.CategoryId.HasValue).ToListAsync();
        bool modified = false;

        foreach (var l in leads)
        {
            var pids = l.GetProductIdList();
            var leadCats = allProds.Where(p => pids.Contains(p.ProductId)).Select(p => p.CategoryId!.Value).Distinct().ToList();

            if (leadCats.Count > 1 || (leadCats.Count == 0 && !l.AssignedTo.HasValue))
            {
                if (!l.IsMultiCategory)
                {
                    l.IsMultiCategory = true;
                    modified = true;
                }
            }
            else if (leadCats.Count == 1)
            {
                if (l.IsMultiCategory)
                {
                    l.IsMultiCategory = false;
                    modified = true;
                }
                if (!l.AssignedTo.HasValue)
                {
                    var rep = activeReps.FirstOrDefault(r => r.CategoryId == leadCats[0]);
                    if (rep != null)
                    {
                        l.AssignedTo = rep.UserId;
                        l.AssignedUser = rep;
                        modified = true;
                    }
                }
            }
        }

        if (modified)
        {
            await _context.SaveChangesAsync();
        }

        if (assignedTo.HasValue)
        {
            leads = leads.Where(l => l.AssignedTo == assignedTo.Value).ToList();
        }

        return leads.Select(l => new
        {
            l.LeadId,
            l.FullName,
            l.Email,
            l.CompanyName,
            l.JobTitle,
            l.Domain,
            l.Industry,
            l.Country,
            l.Phone,
            l.Quantity,
            l.Timeline,
            l.BusinessRequirement,
            l.Source,
            l.Status,
            l.Score,
            l.Qualification,
            l.AssignedTo,
            AssignedSalespersonName = l.AssignedUser?.FullName,
            AssignedSalespersonEmail = l.AssignedUser?.Email,
            AssignedCategoryName = l.AssignedUser?.Category?.CategoryName,
            l.IsMultiCategory,
            l.NextFollowUpDate,
            l.Notes,
            l.CreatedAt,
            l.UpdatedAt,
            ProductIds = l.GetProductIdList(),
            Visitor = l.Visitor != null ? new
            {
                l.Visitor.AnonymousId,
                l.Visitor.FirstSeenAt,
                l.Visitor.LastSeenAt
            } : null,
            Prospect = l.Prospect != null ? new
            {
                l.Prospect.ProspectId,
                l.Prospect.Name,
                l.Prospect.Email,
                l.Prospect.Status
            } : null
        }).ToList<object>();
    }

    public async Task<object?> GetLeadByIdAsync(int id)
    {
        var lead = await _context.Leads
            .Include(l => l.Visitor)
                .ThenInclude(v => v!.Activities)
            .Include(l => l.Prospect)
                .ThenInclude(p => p!.ProfessionalProfile)
            .Include(l => l.Prospect)
                .ThenInclude(p => p!.Company)
                    .ThenInclude(c => c!.Enrichment)
            .Include(l => l.AssignedUser)
                .ThenInclude(u => u!.Category)
            .Include(l => l.ScoreHistories)
            .Include(l => l.StatusHistories)
            .Include(l => l.LeadNotes)
            .Include(l => l.Activities)
            .Include(l => l.Handoffs)
            .FirstOrDefaultAsync(l => l.LeadId == id);

        if (lead == null) return null;

        return new
        {
            lead.LeadId,
            lead.FullName,
            lead.Email,
            lead.CompanyName,
            lead.JobTitle,
            lead.Domain,
            lead.Industry,
            lead.Country,
            lead.Phone,
            lead.Quantity,
            lead.Timeline,
            lead.BusinessRequirement,
            lead.Source,
            lead.Status,
            lead.Score,
            lead.Qualification,
            lead.AssignedTo,
            AssignedSalespersonName = lead.AssignedUser?.FullName,
            AssignedSalespersonEmail = lead.AssignedUser?.Email,
            AssignedCategoryName = lead.AssignedUser?.Category?.CategoryName,
            lead.IsMultiCategory,
            lead.NextFollowUpDate,
            lead.Notes,
            lead.CreatedAt,
            lead.UpdatedAt,
            ProductIds = lead.GetProductIdList(),
            Visitor = lead.Visitor != null ? new
            {
                lead.Visitor.AnonymousId,
                lead.Visitor.FirstSeenAt,
                lead.Visitor.LastSeenAt,
                Activities = lead.Visitor.Activities.OrderByDescending(a => a.Timestamp).Select(a => new
                {
                    a.ActivityId,
                    a.ActivityType,
                    a.PageUrl,
                    a.Timestamp
                })
            } : null,
            Prospect = lead.Prospect != null ? new
            {
                lead.Prospect.ProspectId,
                lead.Prospect.Name,
                lead.Prospect.Email,
                lead.Prospect.JobTitle,
                lead.Prospect.LinkedInUrl,
                ProfessionalProfile = lead.Prospect.ProfessionalProfile != null ? new
                {
                    lead.Prospect.ProfessionalProfile.Title,
                    lead.Prospect.ProfessionalProfile.Seniority,
                    lead.Prospect.ProfessionalProfile.Function,
                    lead.Prospect.ProfessionalProfile.Location,
                    lead.Prospect.ProfessionalProfile.Summary
                } : null,
                Company = lead.Prospect.Company != null ? new
                {
                    lead.Prospect.Company.Name,
                    lead.Prospect.Company.Domain,
                    lead.Prospect.Company.Industry,
                    lead.Prospect.Company.Size,
                    Enrichment = lead.Prospect.Company.Enrichment != null ? new
                    {
                        lead.Prospect.Company.Enrichment.Growth,
                        lead.Prospect.Company.Enrichment.PublicSignals
                    } : null
                } : null
            } : null,
            NotesList = lead.LeadNotes.OrderByDescending(n => n.CreatedAt).Select(n => new
            {
                n.LeadNoteId,
                n.NoteText,
                n.CreatedBy,
                n.CreatedAt
            }),
            StatusHistories = lead.StatusHistories.OrderByDescending(s => s.ChangedAt).Select(s => new
            {
                s.LeadStatusHistoryId,
                s.OldStatus,
                s.NewStatus,
                s.ChangedBy,
                s.Reason,
                s.ChangedAt
            }),
            Activities = lead.Activities.OrderByDescending(a => a.CreatedAt).Select(a => new
            {
                a.LeadActivityId,
                a.ActivityType,
                a.Description,
                a.CreatedBy,
                ActivityDate = a.CreatedAt
            }),
            ScoreHistories = lead.ScoreHistories.OrderByDescending(sh => sh.Timestamp).Select(sh => new
            {
                sh.LeadScoreHistoryId,
                sh.RuleName,
                sh.EventType,
                sh.Delta,
                sh.TotalScore,
                sh.Reason,
                sh.Timestamp
            }),
            Handoffs = lead.Handoffs.OrderByDescending(h => h.CreatedAt).Select(h => new
            {
                h.LeadHandoffId,
                h.Destination,
                h.Status,
                h.HandedOffAt,
                h.Retries,
                h.ErrorMessage
            })
        };
    }

    public async Task<Lead> UpdateLeadAsync(int id, LeadUpdateDto dto, string? changedBy = "User")
    {
        var lead = await _context.Leads.FindAsync(id);
        if (lead == null) throw new ArgumentException("Lead not found.");

        // Status Update with history tracking
        if (!string.IsNullOrEmpty(dto.Status) && dto.Status != lead.Status)
        {
            var oldStatus = lead.Status;
            lead.Status = dto.Status;

            _context.LeadStatusHistories.Add(new LeadStatusHistory
            {
                LeadId = id,
                OldStatus = oldStatus,
                NewStatus = dto.Status,
                ChangedBy = changedBy,
                Reason = "Status updated in CRM",
                ChangedAt = DateTime.UtcNow
            });

            _context.LeadActivities.Add(new LeadActivity
            {
                LeadId = id,
                ActivityType = "STATUS_CHANGE",
                Description = $"Pipeline status changed from '{oldStatus}' to '{dto.Status}'",
                CreatedBy = changedBy,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!string.IsNullOrEmpty(dto.Qualification)) lead.Qualification = dto.Qualification;
        if (dto.Score.HasValue) lead.Score = dto.Score.Value;

        // Next Follow-Up Date update
        if (dto.NextFollowUpDate.HasValue && dto.NextFollowUpDate != lead.NextFollowUpDate)
        {
            lead.NextFollowUpDate = dto.NextFollowUpDate.Value;
            _context.LeadActivities.Add(new LeadActivity
            {
                LeadId = id,
                ActivityType = "FOLLOW_UP_SCHEDULED",
                Description = $"Next follow-up date scheduled for {dto.NextFollowUpDate.Value:yyyy-MM-dd}",
                CreatedBy = changedBy,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Notes update
        if (!string.IsNullOrEmpty(dto.Notes) && dto.Notes != lead.Notes)
        {
            lead.Notes = dto.Notes;
            _context.LeadNotes.Add(new LeadNote
            {
                LeadId = id,
                NoteText = dto.Notes,
                CreatedBy = changedBy,
                CreatedAt = DateTime.UtcNow
            });

            _context.LeadActivities.Add(new LeadActivity
            {
                LeadId = id,
                ActivityType = "NOTE",
                Description = dto.Notes,
                CreatedBy = changedBy,
                CreatedAt = DateTime.UtcNow
            });
        }

        // AssignedTo update
        if (dto.AssignedTo.HasValue && dto.AssignedTo != lead.AssignedTo)
        {
            lead.AssignedTo = dto.AssignedTo.Value == 0 ? null : dto.AssignedTo.Value;
            var assignedUser = lead.AssignedTo.HasValue ? await _context.Users.FindAsync(lead.AssignedTo.Value) : null;
            _context.LeadActivities.Add(new LeadActivity
            {
                LeadId = id,
                ActivityType = "ASSIGNMENT",
                Description = lead.AssignedTo.HasValue 
                    ? $"Lead assigned to {assignedUser?.FullName ?? $"Sales Rep #{lead.AssignedTo.Value}"}"
                    : "Lead unassigned",
                CreatedBy = changedBy,
                CreatedAt = DateTime.UtcNow
            });
        }

        lead.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return lead;
    }
}