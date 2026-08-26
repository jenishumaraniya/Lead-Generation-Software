using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CrmLeadTool.Api.Services;

public class DuplicateService
{
    private readonly AppDbContext _context;

    public DuplicateService(AppDbContext context)
    {
        _context = context;
    }

    public static string NormalizeEmail(string? email) => email?.Trim().ToLowerInvariant() ?? string.Empty;

    public static string NormalizePhone(string? phone) =>
        string.IsNullOrEmpty(phone) ? string.Empty : Regex.Replace(phone, @"[^\d]", "");

    public static string NormalizeText(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : Regex.Replace(text.Trim().ToLowerInvariant(), @"\s+", " ");

    public async Task<Lead?> FindDuplicateLeadAsync(string? email, string? phone = null, string? fullName = null, string? companyName = null)
    {
        var normEmail = NormalizeEmail(email);
        if (!string.IsNullOrEmpty(normEmail))
        {
            var byEmail = await _context.Leads.FirstOrDefaultAsync(l => l.Email.ToLower() == normEmail);
            if (byEmail != null) return byEmail;
        }

        var normPhone = NormalizePhone(phone);
        if (!string.IsNullOrEmpty(normPhone) && normPhone.Length >= 7)
        {
            var byPhone = await _context.Leads.ToListAsync();
            var matched = byPhone.FirstOrDefault(l => NormalizePhone(l.Phone) == normPhone);
            if (matched != null) return matched;
        }

        var normName = NormalizeText(fullName);
        var normComp = NormalizeText(companyName);
        if (!string.IsNullOrEmpty(normName) && !string.IsNullOrEmpty(normComp))
        {
            var byNameAndComp = await _context.Leads.ToListAsync();
            var matched = byNameAndComp.FirstOrDefault(l =>
                NormalizeText(l.FullName) == normName && NormalizeText(l.CompanyName) == normComp);
            if (matched != null) return matched;
        }

        return null;
    }

    public async Task<Prospect?> FindProspectByEmailAsync(string? email)
    {
        var normEmail = NormalizeEmail(email);
        if (string.IsNullOrEmpty(normEmail)) return null;

        return await _context.Prospects.FirstOrDefaultAsync(p => p.Email.ToLower() == normEmail);
    }

    public async Task<Prospect?> FindDuplicateProspectAsync(string? email, string? phone = null, string? name = null, string? companyName = null)
    {
        var normEmail = NormalizeEmail(email);
        if (!string.IsNullOrEmpty(normEmail))
        {
            var byEmail = await _context.Prospects.FirstOrDefaultAsync(p => p.Email.ToLower() == normEmail);
            if (byEmail != null) return byEmail;
        }

        var normPhone = NormalizePhone(phone);
        if (!string.IsNullOrEmpty(normPhone) && normPhone.Length >= 7)
        {
            var prospects = await _context.Prospects.ToListAsync();
            var matched = prospects.FirstOrDefault(p => NormalizePhone(p.Phone) == normPhone);
            if (matched != null) return matched;
        }

        return null;
    }
}