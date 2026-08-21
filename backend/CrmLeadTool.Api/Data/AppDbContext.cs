using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Sprint 1
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<VisitorActivity> VisitorActivities => Set<VisitorActivity>();

    // Sprint 2
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Prospect> Prospects => Set<Prospect>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<SequenceStep> SequenceSteps => Set<SequenceStep>();
    public DbSet<CampaignRecipient> CampaignRecipients => Set<CampaignRecipient>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<EmailEvent> EmailEvents => Set<EmailEvent>();

    // Sprint 3 (NEW)
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadActivity> LeadActivities => Set<LeadActivity>();
    public DbSet<LeadNote> LeadNotes => Set<LeadNote>();
    public DbSet<LeadStatusHistory> LeadStatusHistories => Set<LeadStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ============================================================
        // SPRINT 1 CONFIGURATIONS
        // ============================================================

        modelBuilder.Entity<Visitor>()
            .HasIndex(v => v.AnonymousId)
            .IsUnique();

        modelBuilder.Entity<Visitor>()
            .HasIndex(v => v.PublicId)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<VisitorActivity>()
            .HasOne(a => a.Visitor)
            .WithMany(v => v.Activities)
            .HasForeignKey(a => a.VisitorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VisitorActivity>()
            .HasOne(a => a.Product)
            .WithMany(p => p.Activities)
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<VisitorActivity>()
            .HasIndex(a => a.VisitorId);

        modelBuilder.Entity<VisitorActivity>()
            .HasIndex(a => a.ProductId);

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.CategoryId);

        // ============================================================
        // SPRINT 2 CONFIGURATIONS
        // ============================================================

        modelBuilder.Entity<Prospect>()
            .HasOne(p => p.Company)
            .WithMany(c => c.Prospects)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Prospect>()
            .HasOne(p => p.Visitor)
            .WithMany()
            .HasForeignKey(p => p.VisitorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SequenceStep>()
            .HasOne(s => s.Campaign)
            .WithMany(c => c.Steps)
            .HasForeignKey(s => s.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CampaignRecipient>()
            .HasOne(cr => cr.Campaign)
            .WithMany(c => c.Recipients)
            .HasForeignKey(cr => cr.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CampaignRecipient>()
            .HasOne(cr => cr.Prospect)
            .WithMany(p => p.CampaignRecipients)
            .HasForeignKey(cr => cr.ProspectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EmailMessage>()
            .HasOne(em => em.CampaignRecipient)
            .WithMany(cr => cr.EmailMessages)
            .HasForeignKey(em => em.CampaignRecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EmailMessage>()
            .HasOne(em => em.SequenceStep)
            .WithMany()
            .HasForeignKey(em => em.SequenceStepId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmailEvent>()
            .HasOne(e => e.EmailMessage)
            .WithMany(em => em.Events)
            .HasForeignKey(e => e.EmailMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Prospect>()
            .HasIndex(p => p.Email);

        modelBuilder.Entity<Prospect>()
            .HasIndex(p => p.Status);

        modelBuilder.Entity<Campaign>()
            .HasIndex(c => c.Status);

        modelBuilder.Entity<CampaignRecipient>()
            .HasIndex(cr => cr.Status);

        modelBuilder.Entity<EmailEvent>()
            .HasIndex(e => e.EventType);

        modelBuilder.Entity<EmailEvent>()
            .HasIndex(e => e.EmailMessageId);

        modelBuilder.Entity<EmailMessage>()
            .HasIndex(em => em.ProviderMessageId);

        modelBuilder.Entity<EmailMessage>()
            .HasIndex(em => em.SentAt);

        // ============================================================
        // SPRINT 3 CONFIGURATIONS (NEW)
        // ============================================================

        // Lead → Visitor
        modelBuilder.Entity<Lead>()
            .HasOne(l => l.Visitor)
            .WithMany()
            .HasForeignKey(l => l.VisitorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Lead → Prospect
        modelBuilder.Entity<Lead>()
            .HasOne(l => l.Prospect)
            .WithMany(p => p.Leads)
            .HasForeignKey(l => l.ProspectId)
            .OnDelete(DeleteBehavior.SetNull);

        // LeadActivity → Lead
        modelBuilder.Entity<LeadActivity>()
            .HasOne(a => a.Lead)
            .WithMany(l => l.Activities)
            .HasForeignKey(a => a.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        // LeadNote → Lead
        modelBuilder.Entity<LeadNote>()
            .HasOne(n => n.Lead)
            .WithMany(l => l.Notes)
            .HasForeignKey(n => n.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        // LeadStatusHistory → Lead
        modelBuilder.Entity<LeadStatusHistory>()
            .HasOne(h => h.Lead)
            .WithMany(l => l.StatusHistory)
            .HasForeignKey(h => h.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        // Sprint 3 Indexes
        modelBuilder.Entity<Lead>()
            .HasIndex(l => l.Email);

        modelBuilder.Entity<Lead>()
            .HasIndex(l => l.Status);

        modelBuilder.Entity<Lead>()
            .HasIndex(l => l.Qualification);

        modelBuilder.Entity<Lead>()
            .HasIndex(l => l.CreatedAt);

        modelBuilder.Entity<LeadActivity>()
            .HasIndex(a => a.LeadId);

        modelBuilder.Entity<LeadActivity>()
            .HasIndex(a => a.ActivityType);

        modelBuilder.Entity<LeadStatusHistory>()
            .HasIndex(h => h.LeadId);
    }
}