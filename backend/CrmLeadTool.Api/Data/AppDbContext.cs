using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<VisitorActivity> VisitorActivities => Set<VisitorActivity>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Prospect> Prospects => Set<Prospect>();
    public DbSet<ProfessionalProfile> ProfessionalProfiles => Set<ProfessionalProfile>();
    public DbSet<CompanyEnrichment> CompanyEnrichments => Set<CompanyEnrichment>();
    public DbSet<EnrichmentRun> EnrichmentRuns => Set<EnrichmentRun>();
    public DbSet<EnrichmentField> EnrichmentFields => Set<EnrichmentField>();
    public DbSet<ScoreRule> ScoreRules => Set<ScoreRule>();
    public DbSet<LeadScoreHistory> LeadScoreHistories => Set<LeadScoreHistory>();
    public DbSet<Suppression> Suppressions => Set<Suppression>();
    public DbSet<LeadHandoff> LeadHandoffs => Set<LeadHandoff>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<SequenceStep> SequenceSteps => Set<SequenceStep>();
    public DbSet<CampaignRecipient> CampaignRecipients => Set<CampaignRecipient>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<EmailEvent> EmailEvents => Set<EmailEvent>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadActivity> LeadActivities => Set<LeadActivity>();
    public DbSet<LeadNote> LeadNotes => Set<LeadNote>();
    public DbSet<LeadStatusHistory> LeadStatusHistories => Set<LeadStatusHistory>();
    public DbSet<AIAnalysis> AIAnalyses => Set<AIAnalysis>();
    public DbSet<AIInsight> AIInsights => Set<AIInsight>();
    public DbSet<AIAnalysisHistory> AIAnalysisHistories => Set<AIAnalysisHistory>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().ToTable("User_CRM");
        modelBuilder.Entity<RefreshToken>().ToTable("RefreshToken_CRM");
        modelBuilder.Entity<AuditLog>().ToTable("AuditLog_CRM");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token);

        modelBuilder.Entity<Category>().ToTable("Category_CRM");
        modelBuilder.Entity<Visitor>().ToTable("Visitor_CRM");
        modelBuilder.Entity<Product>().ToTable("Product_CRM");
        modelBuilder.Entity<VisitorActivity>().ToTable("VisitorActivity_CRM");
        modelBuilder.Entity<Company>().ToTable("Company_CRM");
        modelBuilder.Entity<Prospect>().ToTable("Prospect_CRM");
        modelBuilder.Entity<ProfessionalProfile>().ToTable("ProfessionalProfile_CRM");
        modelBuilder.Entity<CompanyEnrichment>().ToTable("CompanyEnrichment_CRM");
        modelBuilder.Entity<EnrichmentRun>().ToTable("EnrichmentRun_CRM");
        modelBuilder.Entity<EnrichmentField>().ToTable("EnrichmentField_CRM");
        modelBuilder.Entity<ScoreRule>().ToTable("ScoreRule_CRM");
        modelBuilder.Entity<LeadScoreHistory>().ToTable("LeadScoreHistory_CRM");
        modelBuilder.Entity<Suppression>().ToTable("Suppression_CRM");
        modelBuilder.Entity<LeadHandoff>().ToTable("LeadHandoff_CRM");
        modelBuilder.Entity<Campaign>().ToTable("Campaign_CRM");
        modelBuilder.Entity<SequenceStep>().ToTable("SequenceStep_CRM");
        modelBuilder.Entity<CampaignRecipient>().ToTable("CampaignRecipient_CRM");
        modelBuilder.Entity<EmailMessage>().ToTable("EmailMessage_CRM");
        modelBuilder.Entity<EmailEvent>().ToTable("EmailEvent_CRM");
        modelBuilder.Entity<Lead>().ToTable("Lead_CRM");
        modelBuilder.Entity<LeadActivity>().ToTable("LeadActivity_CRM");
        modelBuilder.Entity<LeadNote>().ToTable("LeadNote_CRM");
        modelBuilder.Entity<LeadStatusHistory>().ToTable("LeadStatusHistory_CRM");
        modelBuilder.Entity<AIAnalysis>().ToTable("AIAnalysis_CRM");
        modelBuilder.Entity<AIInsight>().ToTable("AIInsight_CRM");
        modelBuilder.Entity<AIAnalysisHistory>().ToTable("AIAnalysisHistory_CRM");


        modelBuilder.Entity<Product>()
            .Property(p => p.Pricing)
            .HasPrecision(18, 4);   
        modelBuilder.Entity<Prospect>()
            .HasOne(p => p.Visitor)
            .WithMany()
            .HasForeignKey(p => p.PublicId)
            .HasPrincipalKey(v => v.PublicId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Prospect>()
            .HasOne(p => p.Company)
            .WithMany(c => c.Prospects)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProfessionalProfile>()
            .HasOne(pp => pp.Prospect)
            .WithOne(p => p.ProfessionalProfile)
            .HasForeignKey<ProfessionalProfile>(pp => pp.ProspectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CompanyEnrichment>()
            .HasOne(ce => ce.Company)
            .WithOne(c => c.Enrichment)
            .HasForeignKey<CompanyEnrichment>(ce => ce.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EnrichmentRun>()
            .HasOne(er => er.Prospect)
            .WithMany(p => p.EnrichmentRuns)
            .HasForeignKey(er => er.ProspectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<EnrichmentField>()
            .HasOne(ef => ef.EnrichmentRun)
            .WithMany(er => er.Fields)
            .HasForeignKey(ef => ef.EnrichmentRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeadScoreHistory>()
            .HasOne(lsh => lsh.Lead)
            .WithMany(l => l.ScoreHistories)
            .HasForeignKey(lsh => lsh.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeadHandoff>()
            .HasOne(lh => lh.Lead)
            .WithMany(l => l.Handoffs)
            .HasForeignKey(lh => lh.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

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

        modelBuilder.Entity<Lead>()
            .HasOne(l => l.AssignedUser)
            .WithMany()
            .HasForeignKey(l => l.AssignedTo)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Category)
            .WithMany()
            .HasForeignKey(u => u.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<LeadNote>()
            .HasOne(n => n.Lead)
            .WithMany(l => l.LeadNotes)
            .HasForeignKey(n => n.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeadStatusHistory>()
            .HasOne(s => s.Lead)
            .WithMany(l => l.StatusHistories)
            .HasForeignKey(s => s.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LeadActivity>()
            .HasOne(a => a.Lead)
            .WithMany(l => l.Activities)
            .HasForeignKey(a => a.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

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

        modelBuilder.Entity<Visitor>()
            .HasIndex(v => v.AnonymousId)
            .IsUnique();

        modelBuilder.Entity<Prospect>()
            .HasIndex(p => p.Email);

        modelBuilder.Entity<Lead>()
            .HasIndex(l => l.Email);

        modelBuilder.Entity<Suppression>()
            .HasIndex(s => s.Email);

       // AIAnalysis → Lead
modelBuilder.Entity<AIAnalysis>()
    .HasOne(a => a.Lead)
    .WithMany()                    // ← no navigation from Lead to AIAnalysis
    .HasForeignKey(a => a.LeadId)
    .OnDelete(DeleteBehavior.Restrict);   // avoids cascade conflicts

// AIInsight → Lead
modelBuilder.Entity<AIInsight>()
    .HasOne(i => i.Lead)
    .WithMany()                    // ← no navigation from Lead to AIInsight
    .HasForeignKey(i => i.LeadId)
    .OnDelete(DeleteBehavior.Restrict);   // fixes the multiple cascade error

// AIInsight → AIAnalysis
modelBuilder.Entity<AIInsight>()
    .HasOne(i => i.Analysis)
    .WithMany(a => a.Insights)     
    .HasForeignKey(i => i.AIAnalysisId)
    .OnDelete(DeleteBehavior.SetNull);

// AIAnalysisHistory → Lead
modelBuilder.Entity<AIAnalysisHistory>()
    .HasOne(h => h.Lead)
    .WithMany()                    
    .HasForeignKey(h => h.LeadId)
    .OnDelete(DeleteBehavior.Restrict);  
    }
}