using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ============================================
    // SPRINT 1 - Visitor, Product, Category, Activity
    // ============================================
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<VisitorActivity> VisitorActivities => Set<VisitorActivity>();

    // ============================================
    // SPRINT 2 - Company, Prospect, Campaign, Email
    // ============================================
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Prospect> Prospects => Set<Prospect>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<SequenceStep> SequenceSteps => Set<SequenceStep>();
    public DbSet<CampaignRecipient> CampaignRecipients => Set<CampaignRecipient>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<EmailEvent> EmailEvents => Set<EmailEvent>();

    // ============================================
    // SPRINT 3 - Lead, LeadActivity, LeadNote, LeadStatusHistory
    // ============================================
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadActivity> LeadActivities => Set<LeadActivity>();
    public DbSet<LeadNote> LeadNotes => Set<LeadNote>();
    public DbSet<LeadStatusHistory> LeadStatusHistories => Set<LeadStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================================================
        // TABLE MAPPINGS (Use existing table names)
        // ============================================================

        // Sprint 1
        modelBuilder.Entity<Category>().ToTable("Category_CRM");
        modelBuilder.Entity<Visitor>().ToTable("Visitor_CRM");
        modelBuilder.Entity<Product>().ToTable("Product_CRM");
        modelBuilder.Entity<VisitorActivity>().ToTable("VisitorActivity_CRM");

        // Sprint 2
        modelBuilder.Entity<Company>().ToTable("Company_CRM");
        modelBuilder.Entity<Prospect>().ToTable("Prospect_CRM");
        modelBuilder.Entity<Campaign>().ToTable("Campaign_CRM");
        modelBuilder.Entity<SequenceStep>().ToTable("SequenceStep_CRM");
        modelBuilder.Entity<CampaignRecipient>().ToTable("CampaignRecipient_CRM");
        modelBuilder.Entity<EmailMessage>().ToTable("EmailMessage_CRM");
        modelBuilder.Entity<EmailEvent>().ToTable("EmailEvent_CRM");

        // Sprint 3
        modelBuilder.Entity<Lead>().ToTable("Lead_CRM");
        modelBuilder.Entity<LeadActivity>().ToTable("LeadActivity_CRM");
        modelBuilder.Entity<LeadNote>().ToTable("LeadNote_CRM");
        modelBuilder.Entity<LeadStatusHistory>().ToTable("LeadStatusHistory_CRM");

        // ============================================================
        // COLUMN MAPPINGS FOR LEAD (Explicit to avoid issues)
        // ============================================================
        modelBuilder.Entity<Lead>(entity =>
        {
            entity.HasKey(e => e.LeadId);
            
            // Map all properties to existing columns
            entity.Property(e => e.VisitorId).HasColumnName("VisitorId");
            entity.Property(e => e.ProspectId).HasColumnName("ProspectId");
            entity.Property(e => e.CompanyName).HasColumnName("CompanyName");
            entity.Property(e => e.FullName).HasColumnName("FullName");
            entity.Property(e => e.Email).HasColumnName("Email");
            entity.Property(e => e.JobTitle).HasColumnName("JobTitle");
            entity.Property(e => e.Domain).HasColumnName("Domain");
            entity.Property(e => e.Industry).HasColumnName("Industry");
            entity.Property(e => e.Country).HasColumnName("Country");
            entity.Property(e => e.Phone).HasColumnName("Phone");
            entity.Property(e => e.ProductIds).HasColumnName("ProductIds");
            entity.Property(e => e.Quantity).HasColumnName("Quantity");
            entity.Property(e => e.Timeline).HasColumnName("Timeline");
            entity.Property(e => e.BusinessRequirement).HasColumnName("BusinessRequirement");
            entity.Property(e => e.Source).HasColumnName("Source");
            entity.Property(e => e.Status).HasColumnName("Status");
            entity.Property(e => e.Score).HasColumnName("Score");
            entity.Property(e => e.Qualification).HasColumnName("Qualification");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
        });

        // ============================================================
        // RELATIONSHIPS
        // ============================================================

        // Sprint 1 Relationships
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

        // Sprint 2 Relationships
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

        // Sprint 3 Relationships
        modelBuilder.Entity<Lead>()
            .HasOne(l => l.Visitor)
            .WithMany()
            .HasForeignKey(l => l.VisitorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Lead>()
            .HasOne(l => l.Prospect)
            .WithMany(p => p.Leads)
            .HasForeignKey(l => l.ProspectId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<LeadActivity>()
            .HasOne(a => a.Lead)
            .WithMany(l => l.Activities)
            .HasForeignKey(a => a.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeadNote>()
            .HasOne(n => n.Lead)
            .WithMany(l => l.Notes)
            .HasForeignKey(n => n.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeadStatusHistory>()
            .HasOne(h => h.Lead)
            .WithMany(l => l.StatusHistory)
            .HasForeignKey(h => h.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        // ============================================================
        // INDEXES (Performance)
        // ============================================================

        // Sprint 1 Indexes
        modelBuilder.Entity<VisitorActivity>()
            .HasIndex(a => a.VisitorId);

        modelBuilder.Entity<VisitorActivity>()
            .HasIndex(a => a.ProductId);

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.CategoryId);

        // Sprint 2 Indexes
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