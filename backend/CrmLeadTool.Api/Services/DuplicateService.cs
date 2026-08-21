using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.DTOs;
using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Services;

public class DuplicateService
{
    private readonly AppDbContext _context;

    public DuplicateService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Lead?> FindDuplicateByEmailAsync(string email)
    {
        if (string.IsNullOrEmpty(email))
            return null;

        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await _context.Leads
            .FirstOrDefaultAsync(l => l.Email.ToLower() == normalizedEmail);
    }

    public async Task<Lead?> FindDuplicateByCompanyAndNameAsync(string companyName, string fullName)
    {
        if (string.IsNullOrEmpty(companyName) || string.IsNullOrEmpty(fullName))
            return null;

        return await _context.Leads
            .FirstOrDefaultAsync(l =>
                l.CompanyName.ToLower() == companyName.Trim().ToLower() &&
                l.FullName.ToLower() == fullName.Trim().ToLower());
    }

    public async Task<Lead?> FindDuplicateByPhoneAsync(string phone)
    {
        if (string.IsNullOrEmpty(phone))
            return null;

        var normalizedPhone = new string(phone.Where(char.IsDigit).ToArray());
        return await _context.Leads
            .FirstOrDefaultAsync(l => new string(l.Phone.Where(char.IsDigit).ToArray()) == normalizedPhone);
    }

    public async Task<List<Lead>> FindAllDuplicatesAsync(LeadSubmitDto dto)
    {
        var duplicates = new List<Lead>();

        var emailDuplicate = await FindDuplicateByEmailAsync(dto.Email);
        if (emailDuplicate != null)
            duplicates.Add(emailDuplicate);

        var companyNameDuplicate = await FindDuplicateByCompanyAndNameAsync(dto.CompanyName, dto.FullName);
        if (companyNameDuplicate != null && !duplicates.Any(d => d.LeadId == companyNameDuplicate.LeadId))
            duplicates.Add(companyNameDuplicate);

        var phoneDuplicate = await FindDuplicateByPhoneAsync(dto.Phone);
        if (phoneDuplicate != null && !duplicates.Any(d => d.LeadId == phoneDuplicate.LeadId))
            duplicates.Add(phoneDuplicate);

        return duplicates;
    }

    public async Task<Prospect?> FindProspectByEmailAsync(string email)
    {
        if (string.IsNullOrEmpty(email))
            return null;

        return await _context.Prospects
            .FirstOrDefaultAsync(p => p.Email.ToLower() == email.Trim().ToLower());
    }
}