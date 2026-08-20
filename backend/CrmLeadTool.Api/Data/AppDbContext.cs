using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Sprint 1 (existing)
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<VisitorActivity> VisitorActivities => Set<VisitorActivity>();
    public DbSet<Lead> Leads => Set<Lead>();  // from Sprint 1/2 (added)

    // Sprint 2 (new)
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Prospect> Prospects => Set<Prospect>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<SequenceStep> SequenceSteps => Set<SequenceStep>();
    public DbSet<CampaignRecipient> CampaignRecipients => Set<CampaignRecipient>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<EmailEvent> EmailEvents => Set<EmailEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ============================================================
        // SPRINT 1 CONFIGURATIONS (keep existing)
        // ============================================================

        // Visitor unique indexes
        modelBuilder.Entity<Visitor>()
            .HasIndex(v => v.AnonymousId)
            .IsUnique();

        modelBuilder.Entity<Visitor>()
            .HasIndex(v => v.PublicId)
            .IsUnique();

        // Product → Category
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // VisitorActivity → Visitor (CASCADE)
        modelBuilder.Entity<VisitorActivity>()
            .HasOne(a => a.Visitor)
            .WithMany(v => v.Activities)
            .HasForeignKey(a => a.VisitorId)
            .OnDelete(DeleteBehavior.Cascade);

        // VisitorActivity → Product (SET NULL)
        modelBuilder.Entity<VisitorActivity>()
            .HasOne(a => a.Product)
            .WithMany(p => p.Activities)
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes (optional but recommended)
        modelBuilder.Entity<VisitorActivity>()
            .HasIndex(a => a.VisitorId);

        modelBuilder.Entity<VisitorActivity>()
            .HasIndex(a => a.ProductId);

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.CategoryId);

        // Lead → Visitor (already existed)
        modelBuilder.Entity<Lead>()
            .HasOne(l => l.Visitor)
            .WithMany()
            .HasForeignKey(l => l.VisitorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Lead → Prospect (new – Sprint 2)
        modelBuilder.Entity<Lead>()
            .HasOne(l => l.Prospect)
            .WithMany(p => p.Leads)
            .HasForeignKey(l => l.ProspectId)
            .OnDelete(DeleteBehavior.SetNull);

        // ============================================================
        // SPRINT 2 CONFIGURATIONS
        // ============================================================

        // Company → Prospects
        modelBuilder.Entity<Prospect>()
            .HasOne(p => p.Company)
            .WithMany(c => c.Prospects)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        // Prospect → Visitor (optional link)
        modelBuilder.Entity<Prospect>()
            .HasOne(p => p.Visitor)
            .WithMany()
            .HasForeignKey(p => p.VisitorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Campaign → SequenceSteps (cascade delete)
        modelBuilder.Entity<SequenceStep>()
            .HasOne(s => s.Campaign)
            .WithMany(c => c.Steps)
            .HasForeignKey(s => s.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        // CampaignRecipient → Campaign
        modelBuilder.Entity<CampaignRecipient>()
            .HasOne(cr => cr.Campaign)
            .WithMany(c => c.Recipients)
            .HasForeignKey(cr => cr.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        // CampaignRecipient → Prospect
        modelBuilder.Entity<CampaignRecipient>()
            .HasOne(cr => cr.Prospect)
            .WithMany(p => p.CampaignRecipients)
            .HasForeignKey(cr => cr.ProspectId)
            .OnDelete(DeleteBehavior.Cascade);

        // EmailMessage → CampaignRecipient
        modelBuilder.Entity<EmailMessage>()
            .HasOne(em => em.CampaignRecipient)
            .WithMany(cr => cr.EmailMessages)
            .HasForeignKey(em => em.CampaignRecipientId)
            .OnDelete(DeleteBehavior.Cascade);

        // EmailMessage → SequenceStep (restrict delete)
        modelBuilder.Entity<EmailMessage>()
            .HasOne(em => em.SequenceStep)
            .WithMany()
            .HasForeignKey(em => em.SequenceStepId)
            .OnDelete(DeleteBehavior.Restrict);

        // EmailEvent → EmailMessage
        modelBuilder.Entity<EmailEvent>()
            .HasOne(e => e.EmailMessage)
            .WithMany(em => em.Events)
            .HasForeignKey(e => e.EmailMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // ============================================================
        // ADDITIONAL INDEXES FOR PERFORMANCE
        // ============================================================

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
    }
}