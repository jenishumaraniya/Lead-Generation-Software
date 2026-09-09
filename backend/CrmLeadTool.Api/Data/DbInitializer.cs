using CrmLeadTool.Api.Models;
using CrmLeadTool.Api.Utils;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        var tables = new[]
        {
            // 1. Categories
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Category_CRM')
              CREATE TABLE Category_CRM (
                  CategoryId INT IDENTITY(1,1) PRIMARY KEY,
                  CategoryName NVARCHAR(255) NOT NULL,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 2. Products
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Product_CRM')
              CREATE TABLE Product_CRM (
                  ProductId INT IDENTITY(1,1) PRIMARY KEY,
                  CategoryId INT NULL,
                  Name NVARCHAR(255) NOT NULL,
                  Description NVARCHAR(MAX) NULL,
                  Pricing DECIMAL(18,2) NOT NULL DEFAULT 0,
                  Features NVARCHAR(MAX) NULL,
                  Specifications NVARCHAR(MAX) NULL,
                  ImageUrl NVARCHAR(MAX) NULL,
                  Status NVARCHAR(50) NOT NULL DEFAULT 'ACTIVE',
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 3. Visitors
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Visitor_CRM')
              CREATE TABLE Visitor_CRM (
                  VisitorId INT IDENTITY(1,1) PRIMARY KEY,
                  PublicId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                  AnonymousId NVARCHAR(255) NOT NULL,
                  ConsentStatus NVARCHAR(50) NOT NULL DEFAULT 'UNKNOWN',
                  FirstSeenAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  LastSeenAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 4. Visitor Activities
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'VisitorActivity_CRM')
              CREATE TABLE VisitorActivity_CRM (
                  ActivityId BIGINT IDENTITY(1,1) PRIMARY KEY,
                  VisitorId INT NOT NULL,
                  ProductId INT NULL,
                  ActivityType NVARCHAR(100) NOT NULL,
                  PageUrl NVARCHAR(500) NULL,
                  Metadata NVARCHAR(MAX) NULL,
                  Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 5. Companies
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Company_CRM')
              CREATE TABLE Company_CRM (
                  CompanyId INT IDENTITY(1,1) PRIMARY KEY,
                  Name NVARCHAR(255) NOT NULL,
                  Domain NVARCHAR(255) NULL,
                  Industry NVARCHAR(255) NULL,
                  Size NVARCHAR(100) NULL,
                  Location NVARCHAR(255) NULL,
                  Description NVARCHAR(MAX) NULL,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 6. Prospects
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Prospect_CRM')
              CREATE TABLE Prospect_CRM (
                  ProspectId INT IDENTITY(1,1) PRIMARY KEY,
                  CompanyId INT NULL,
                  PublicId UNIQUEIDENTIFIER NULL,
                  Email NVARCHAR(255) NOT NULL,
                  Name NVARCHAR(255) NOT NULL DEFAULT '',
                  JobTitle NVARCHAR(255) NULL,
                  Phone NVARCHAR(50) NULL,
                  LinkedInUrl NVARCHAR(500) NULL,
                  Source NVARCHAR(100) NOT NULL DEFAULT 'MANUAL',
                  Status NVARCHAR(50) NOT NULL DEFAULT 'NEW',
                  Score INT NULL DEFAULT 0,
                  Qualification NVARCHAR(100) NULL,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 7. Leads
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Lead_CRM')
              CREATE TABLE Lead_CRM (
                  LeadId INT IDENTITY(1,1) PRIMARY KEY,
                  VisitorId INT NULL,
                  ProspectId INT NULL,
                  AssignedTo INT NULL,
                  Notes NVARCHAR(MAX) NULL,
                  NextFollowUpDate DATETIME2 NULL,
                  IsMultiCategory BIT NOT NULL DEFAULT 0,
                  CompanyName NVARCHAR(255) NOT NULL DEFAULT '',
                  FullName NVARCHAR(255) NOT NULL DEFAULT '',
                  Email NVARCHAR(255) NOT NULL DEFAULT '',
                  JobTitle NVARCHAR(255) NOT NULL DEFAULT '',
                  Domain NVARCHAR(255) NOT NULL DEFAULT '',
                  Industry NVARCHAR(255) NOT NULL DEFAULT '',
                  Country NVARCHAR(100) NOT NULL DEFAULT '',
                  Phone NVARCHAR(50) NOT NULL DEFAULT '',
                  ProductIds NVARCHAR(MAX) NOT NULL DEFAULT '[]',
                  Quantity INT NULL,
                  ProductQuantities NVARCHAR(MAX) NULL,
                  Timeline NVARCHAR(100) NOT NULL DEFAULT '',
                  BusinessRequirement NVARCHAR(MAX) NOT NULL DEFAULT '',
                  Source NVARCHAR(100) NULL,
                  Status NVARCHAR(50) NOT NULL DEFAULT 'NEW',
                  Score INT NULL DEFAULT 0,
                  Qualification NVARCHAR(100) NULL,
                  PriorityLevel NVARCHAR(50) NULL,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 8. Users
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'User_CRM')
              CREATE TABLE User_CRM (
                  UserId INT IDENTITY(1,1) PRIMARY KEY,
                  FullName NVARCHAR(255) NOT NULL,
                  Email NVARCHAR(255) NOT NULL UNIQUE,
                  PasswordHash NVARCHAR(500) NOT NULL,
                  Salt NVARCHAR(255) NOT NULL,
                  Role NVARCHAR(50) NOT NULL DEFAULT 'SALES_REP',
                  IsActive BIT NOT NULL DEFAULT 1,
                  FailedLoginAttempts INT NOT NULL DEFAULT 0,
                  LockoutEnd DATETIME2 NULL,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  LastLoginAt DATETIME2 NULL,
                  CategoryId INT NULL
              )",

            // 9. Refresh Tokens
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RefreshToken_CRM')
              CREATE TABLE RefreshToken_CRM (
                  RefreshTokenId INT IDENTITY(1,1) PRIMARY KEY,
                  UserId INT NOT NULL,
                  Token NVARCHAR(500) NOT NULL,
                  ExpiresAt DATETIME2 NOT NULL,
                  IsRevoked BIT NOT NULL DEFAULT 0,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  RevokedAt DATETIME2 NULL,
                  ReplacedByToken NVARCHAR(500) NULL
              )",

            // 10. Audit Logs
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLog_CRM')
              CREATE TABLE AuditLog_CRM (
                  AuditLogId INT IDENTITY(1,1) PRIMARY KEY,
                  UserId INT NULL,
                  UserEmail NVARCHAR(255) NOT NULL,
                  Action NVARCHAR(100) NOT NULL,
                  EntityName NVARCHAR(100) NOT NULL,
                  EntityId NVARCHAR(100) NULL,
                  Details NVARCHAR(MAX) NULL,
                  IpAddress NVARCHAR(100) NULL,
                  Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 11. Score Rules
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ScoreRule_CRM')
              CREATE TABLE ScoreRule_CRM (
                  ScoreRuleId INT IDENTITY(1,1) PRIMARY KEY,
                  Name NVARCHAR(255) NOT NULL,
                  EventType NVARCHAR(100) NOT NULL,
                  Category NVARCHAR(100) NOT NULL DEFAULT 'ENGAGEMENT',
                  Direction NVARCHAR(50) NOT NULL DEFAULT 'ADD',
                  Points INT NOT NULL,
                  IsActive BIT NOT NULL DEFAULT 1,
                  Description NVARCHAR(500) NULL,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 12. Lead Score History
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LeadScoreHistory_CRM')
              CREATE TABLE LeadScoreHistory_CRM (
                  LeadScoreHistoryId INT IDENTITY(1,1) PRIMARY KEY,
                  LeadId INT NOT NULL,
                  RuleId INT NULL,
                  RuleName NVARCHAR(255) NOT NULL,
                  EventType NVARCHAR(100) NOT NULL,
                  Delta INT NOT NULL,
                  TotalScore INT NOT NULL,
                  Reason NVARCHAR(500) NULL,
                  Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 13. Professional Profiles
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ProfessionalProfile_CRM')
              CREATE TABLE ProfessionalProfile_CRM (
                  ProfessionalProfileId INT IDENTITY(1,1) PRIMARY KEY,
                  ProspectId INT NOT NULL,
                  LinkedInReference NVARCHAR(500) NULL,
                  Title NVARCHAR(255) NULL,
                  Seniority NVARCHAR(100) NULL,
                  [Function] NVARCHAR(100) NULL,
                  Location NVARCHAR(255) NULL,
                  Summary NVARCHAR(MAX) NULL,
                  Skills NVARCHAR(500) NULL,
                  ExperienceYears NVARCHAR(100) NULL,
                  SourceTimestamp DATETIME2 NULL,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 14. Company Enrichment
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CompanyEnrichment_CRM')
              CREATE TABLE CompanyEnrichment_CRM (
                  CompanyEnrichmentId INT IDENTITY(1,1) PRIMARY KEY,
                  CompanyId INT NOT NULL,
                  Industry NVARCHAR(255) NULL,
                  Size NVARCHAR(100) NULL,
                  Growth NVARCHAR(255) NULL,
                  PublicSignals NVARCHAR(MAX) NULL,
                  Technologies NVARCHAR(500) NULL,
                  Location NVARCHAR(255) NULL,
                  Description NVARCHAR(MAX) NULL,
                  SourceTimestamp DATETIME2 NULL,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 15. Enrichment Runs
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EnrichmentRun_CRM')
              CREATE TABLE EnrichmentRun_CRM (
                  EnrichmentRunId INT IDENTITY(1,1) PRIMARY KEY,
                  ProspectId INT NOT NULL,
                  Source NVARCHAR(100) NOT NULL DEFAULT 'LINKEDIN',
                  StartedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  CompletedAt DATETIME2 NULL,
                  Status NVARCHAR(50) NOT NULL DEFAULT 'QUEUED',
                  Error NVARCHAR(MAX) NULL,
                  RawPayload NVARCHAR(MAX) NULL
              )",

            // 16. Enrichment Fields
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EnrichmentField_CRM')
              CREATE TABLE EnrichmentField_CRM (
                  EnrichmentFieldId INT IDENTITY(1,1) PRIMARY KEY,
                  EnrichmentRunId INT NOT NULL,
                  FieldName NVARCHAR(255) NOT NULL,
                  Value NVARCHAR(MAX) NULL,
                  Source NVARCHAR(100) NOT NULL DEFAULT 'LINKEDIN',
                  Confidence NVARCHAR(50) NOT NULL DEFAULT 'HIGH',
                  IsAiInferred BIT NOT NULL DEFAULT 0,
                  Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 17. Suppression
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Suppression_CRM')
              CREATE TABLE Suppression_CRM (
                  SuppressionId INT IDENTITY(1,1) PRIMARY KEY,
                  Email NVARCHAR(255) NOT NULL,
                  ProspectId INT NULL,
                  Reason NVARCHAR(100) NOT NULL DEFAULT 'OPT_OUT',
                  Notes NVARCHAR(500) NULL,
                  IsActive BIT NOT NULL DEFAULT 1,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 18. Lead Handoff
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LeadHandoff_CRM')
              CREATE TABLE LeadHandoff_CRM (
                  LeadHandoffId INT IDENTITY(1,1) PRIMARY KEY,
                  LeadId INT NOT NULL,
                  Destination NVARCHAR(100) NOT NULL DEFAULT 'SALES_CRM',
                  Status NVARCHAR(50) NOT NULL DEFAULT 'PENDING',
                  PayloadJson NVARCHAR(MAX) NULL,
                  ResponseJson NVARCHAR(MAX) NULL,
                  ErrorMessage NVARCHAR(MAX) NULL,
                  Retries INT NOT NULL DEFAULT 0,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  HandedOffAt DATETIME2 NULL
              )",

            // 19. Campaigns
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Campaign_CRM')
              CREATE TABLE Campaign_CRM (
                  CampaignId INT IDENTITY(1,1) PRIMARY KEY,
                  Name NVARCHAR(255) NOT NULL,
                  Description NVARCHAR(MAX) NULL,
                  Status NVARCHAR(50) NOT NULL DEFAULT 'DRAFT',
                  ScheduleStartDate DATETIME2 NULL,
                  ScheduleEndDate DATETIME2 NULL,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 20. Sequence Steps
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SequenceStep_CRM')
              CREATE TABLE SequenceStep_CRM (
                  SequenceStepId INT IDENTITY(1,1) PRIMARY KEY,
                  CampaignId INT NOT NULL,
                  StepNumber INT NOT NULL,
                  Name NVARCHAR(255) NOT NULL,
                  Subject NVARCHAR(255) NOT NULL,
                  Body NVARCHAR(MAX) NOT NULL,
                  DelayDays INT NOT NULL DEFAULT 0,
                  DelayHours INT NOT NULL DEFAULT 0,
                  IsActive BIT NOT NULL DEFAULT 1,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 21. Campaign Recipients
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CampaignRecipient_CRM')
              CREATE TABLE CampaignRecipient_CRM (
                  CampaignRecipientId INT IDENTITY(1,1) PRIMARY KEY,
                  CampaignId INT NOT NULL,
                  ProspectId INT NOT NULL,
                  Status NVARCHAR(50) NOT NULL DEFAULT 'ENROLLED',
                  CurrentStep INT NOT NULL DEFAULT 1,
                  EnrolledAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  LastActivityAt DATETIME2 NULL,
                  CompletedAt DATETIME2 NULL
              )",

            // 22. Email Messages
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EmailMessage_CRM')
              CREATE TABLE EmailMessage_CRM (
                  EmailMessageId INT IDENTITY(1,1) PRIMARY KEY,
                  CampaignRecipientId INT NOT NULL,
                  SequenceStepId INT NOT NULL,
                  FromEmail NVARCHAR(255) NOT NULL DEFAULT '',
                  ToEmail NVARCHAR(255) NOT NULL,
                  Subject NVARCHAR(500) NOT NULL DEFAULT '',
                  Body NVARCHAR(MAX) NOT NULL DEFAULT '',
                  ProviderMessageId NVARCHAR(255) NULL,
                  SentAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  Status NVARCHAR(50) NOT NULL DEFAULT 'SENT',
                  OpenedAt DATETIME2 NULL,
                  ClickedAt DATETIME2 NULL,
                  RepliedAt DATETIME2 NULL
              )",

            // 23. Email Events
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EmailEvent_CRM')
              CREATE TABLE EmailEvent_CRM (
                  EmailEventId INT IDENTITY(1,1) PRIMARY KEY,
                  EmailMessageId INT NOT NULL,
                  EventType NVARCHAR(50) NOT NULL,
                  EventTimestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  UserAgent NVARCHAR(500) NULL,
                  IpAddress NVARCHAR(100) NULL,
                  ClickUrl NVARCHAR(MAX) NULL,
                  Metadata NVARCHAR(MAX) NULL,
                  ProviderEventId NVARCHAR(255) NULL
              )",

            // 24. Lead Notes
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LeadNote_CRM')
              CREATE TABLE LeadNote_CRM (
                  LeadNoteId INT IDENTITY(1,1) PRIMARY KEY,
                  LeadId INT NOT NULL,
                  NoteText NVARCHAR(MAX) NOT NULL,
                  CreatedBy NVARCHAR(255) NULL,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 25. Lead Status History
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LeadStatusHistory_CRM')
              CREATE TABLE LeadStatusHistory_CRM (
                  LeadStatusHistoryId INT IDENTITY(1,1) PRIMARY KEY,
                  LeadId INT NOT NULL,
                  OldStatus NVARCHAR(50) NULL,
                  NewStatus NVARCHAR(50) NOT NULL,
                  ChangedBy NVARCHAR(255) NULL,
                  Reason NVARCHAR(500) NULL,
                  ChangedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 26. Lead Activities
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LeadActivity_CRM')
              CREATE TABLE LeadActivity_CRM (
                  LeadActivityId INT IDENTITY(1,1) PRIMARY KEY,
                  LeadId INT NOT NULL,
                  ActivityType NVARCHAR(100) NOT NULL,
                  Description NVARCHAR(MAX) NULL,
                  Metadata NVARCHAR(MAX) NULL,
                  CreatedBy NVARCHAR(255) NULL,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 27. AI Analysis
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AIAnalysis_CRM')
              CREATE TABLE AIAnalysis_CRM (
                  AIAnalysisId INT IDENTITY(1,1) PRIMARY KEY,
                  LeadId INT NOT NULL,
                  Intent NVARCHAR(100) NOT NULL DEFAULT 'AWARENESS',
                  ConfidenceScore DECIMAL(5,2) NOT NULL DEFAULT 0,
                  LeadSummary NVARCHAR(MAX) NOT NULL DEFAULT '',
                  PriorityRecommendation NVARCHAR(50) NOT NULL DEFAULT 'MEDIUM',
                  RecommendedNextAction NVARCHAR(MAX) NOT NULL DEFAULT '',
                  ProfessionalSummary NVARCHAR(MAX) NULL,
                  LikelyIndustry NVARCHAR(255) NULL,
                  CompanySize NVARCHAR(100) NULL,
                  LikelyLocation NVARCHAR(255) NULL,
                  PotentialRole NVARCHAR(255) NULL,
                  ModelVersion NVARCHAR(100) NULL,
                  RawResponse NVARCHAR(MAX) NULL,
                  AnalysisDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 28. AI Insights
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AIInsight_CRM')
              CREATE TABLE AIInsight_CRM (
                  AIInsightId INT IDENTITY(1,1) PRIMARY KEY,
                  LeadId INT NOT NULL,
                  AIAnalysisId INT NULL,
                  InsightType NVARCHAR(100) NOT NULL DEFAULT '',
                  InsightText NVARCHAR(MAX) NOT NULL DEFAULT '',
                  ConfidenceScore DECIMAL(5,2) NULL,
                  IsAccepted BIT NOT NULL DEFAULT 0,
                  IsUsed BIT NOT NULL DEFAULT 0,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

            // 29. AI Analysis History
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AIAnalysisHistory_CRM')
              CREATE TABLE AIAnalysisHistory_CRM (
                  AIAnalysisHistoryId INT IDENTITY(1,1) PRIMARY KEY,
                  LeadId INT NOT NULL,
                  PreviousIntent NVARCHAR(100) NULL,
                  NewIntent NVARCHAR(100) NOT NULL DEFAULT '',
                  PreviousPriority NVARCHAR(50) NULL,
                  NewPriority NVARCHAR(50) NOT NULL DEFAULT '',
                  ChangedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  ChangedBy NVARCHAR(255) NULL,
                  Reason NVARCHAR(MAX) NULL
              )",

            // 30. Notifications
            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Notification_CRM')
              CREATE TABLE Notification_CRM (
                  NotificationId INT IDENTITY(1,1) PRIMARY KEY,
                  UserId INT NULL,
                  TargetRole NVARCHAR(50) NULL,
                  Title NVARCHAR(200) NOT NULL,
                  Message NVARCHAR(1000) NOT NULL,
                  Type NVARCHAR(50) NOT NULL DEFAULT 'GENERAL',
                  LeadId INT NULL,
                  IsRead BIT NOT NULL DEFAULT 0,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )"
        };

        foreach (var sql in tables)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbInitializer] SQL error: {ex.Message}");
            }
        }

        // Idempotent column migrations — add new columns to existing tables without dropping data
        var columnMigrations = new[]
        {
            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Lead_CRM' AND COLUMN_NAME='AssignedTo')
              ALTER TABLE Lead_CRM ADD AssignedTo INT NULL",

            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Lead_CRM' AND COLUMN_NAME='Notes')
              ALTER TABLE Lead_CRM ADD Notes NVARCHAR(MAX) NULL",

            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Lead_CRM' AND COLUMN_NAME='NextFollowUpDate')
              ALTER TABLE Lead_CRM ADD NextFollowUpDate DATETIME2 NULL",

            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Lead_CRM' AND COLUMN_NAME='IsMultiCategory')
              ALTER TABLE Lead_CRM ADD IsMultiCategory BIT NOT NULL DEFAULT 0",

            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Lead_CRM' AND COLUMN_NAME='ProductQuantities')
              ALTER TABLE Lead_CRM ADD ProductQuantities NVARCHAR(MAX) NULL",

            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='User_CRM' AND COLUMN_NAME='CategoryId')
              ALTER TABLE User_CRM ADD CategoryId INT NULL",

            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Campaign_CRM' AND COLUMN_NAME='ScheduleStartDate')
              ALTER TABLE Campaign_CRM ADD ScheduleStartDate DATETIME2 NULL",

            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Campaign_CRM' AND COLUMN_NAME='ScheduleEndDate')
              ALTER TABLE Campaign_CRM ADD ScheduleEndDate DATETIME2 NULL",

            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Product_CRM' AND COLUMN_NAME='ImageUrl')
              ALTER TABLE Product_CRM ADD ImageUrl NVARCHAR(MAX) NULL",

            // Product_CRM: Check constraint ensuring Pricing > 0
            @"IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Product_Pricing_Positive')
              ALTER TABLE Product_CRM ADD CONSTRAINT CK_Product_Pricing_Positive CHECK (Pricing > 0)"
        };

        foreach (var sql in columnMigrations)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DbInitializer] Column migration error: {ex.Message}");
            }
        }

        // Seed Default Users
        try
        {
            if (!await context.Users.AnyAsync(u => u.Email == "admin@leadgen.com"))
            {
                var (hash, salt) = PasswordHasher.HashPassword("Admin@123");
                context.Users.Add(new User
                {
                    FullName = "Platform Administrator",
                    Email = "admin@leadgen.com",
                    PasswordHash = hash,
                    Salt = salt,
                    Role = "ADMIN",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!await context.Users.AnyAsync(u => u.Email == "sales@leadgen.com"))
            {
                var (hash, salt) = PasswordHasher.HashPassword("Sales@123");
                context.Users.Add(new User
                {
                    FullName = "Alex Carter (Sales Rep)",
                    Email = "sales@leadgen.com",
                    PasswordHash = hash,
                    Salt = salt,
                    Role = "SALES_REP",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!await context.Users.AnyAsync(u => u.Email == "sutharharshit695@gmail.com"))
            {
                var (hash, salt) = PasswordHasher.HashPassword("Sales@123");
                context.Users.Add(new User
                {
                    FullName = "Harshit Suthar (Sales Rep)",
                    Email = "sutharharshit695@gmail.com",
                    PasswordHash = hash,
                    Salt = salt,
                    Role = "SALES_REP",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var allUsers = await context.Users.ToListAsync();
            var (defaultSalesHash, defaultSalesSalt) = PasswordHasher.HashPassword("Sales@123");
            foreach (var u in allUsers)
            {
                u.FailedLoginAttempts = 0;
                u.LockoutEnd = null;
                u.IsActive = true;
                if (string.IsNullOrEmpty(u.PasswordHash) || string.IsNullOrEmpty(u.Salt))
                {
                    u.PasswordHash = defaultSalesHash;
                    u.Salt = defaultSalesSalt;
                }
            }
            await context.SaveChangesAsync();

            // Seed Default Categories if missing
            var defaultCategoryNames = new[] { "Enterprise Laptops", "Workstation Desktops", "Enterprise Servers", "Networking Systems", "Cloud Solutions" };
            foreach (var catName in defaultCategoryNames)
            {
                if (!await context.Categories.AnyAsync(c => c.CategoryName == catName))
                {
                    context.Categories.Add(new Category
                    {
                        CategoryName = catName,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            await context.SaveChangesAsync();

            // Seed Default Products if table is empty
            if (!await context.Products.AnyAsync())
            {
                var cats = await context.Categories.ToListAsync();
                var laptopCat = cats.FirstOrDefault(c => c.CategoryName.Contains("Laptop"))?.CategoryId;
                var desktopCat = cats.FirstOrDefault(c => c.CategoryName.Contains("Desktop"))?.CategoryId;
                var serverCat = cats.FirstOrDefault(c => c.CategoryName.Contains("Server"))?.CategoryId;
                var networkCat = cats.FirstOrDefault(c => c.CategoryName.Contains("Network"))?.CategoryId;
                var cloudCat = cats.FirstOrDefault(c => c.CategoryName.Contains("Cloud"))?.CategoryId;

                context.Products.AddRange(new[]
                {
                    new Product
                    {
                        Name = "Enterprise Pro Laptop 15",
                        CategoryId = laptopCat,
                        Pricing = 65000,
                        Description = "High-performance enterprise laptop equipped for executive workflows, data processing, and day-long battery endurance.",
                        Features = "[\"Intel Core i7 13th Gen / 16GB DDR5 RAM\",\"512GB NVMe M.2 High-Speed SSD\",\"15.6\\\" Full HD Anti-Glare IPS Display\",\"Hardware TPM 2.0 Security & Fingerprint Reader\"]",
                        Specifications = "[\"Processor: Intel Core i7-13700H (14 Cores)\",\"Memory: 16GB DDR5 4800MHz\",\"Storage: 512GB PCIe Gen4 NVMe\",\"Display: 15.6\\\" 1920x1080 300 nits IPS\",\"Battery: 70Wh Lithium-Polymer (10+ Hours)\",\"OS: Windows 11 Pro 64-Bit\"]",
                        Status = "ACTIVE",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Product
                    {
                        Name = "Workstation Desktop Precision",
                        CategoryId = desktopCat,
                        Pricing = 55000,
                        Description = "Engineered workstation tower designed for heavy CAD modeling, machine learning computation, and multitasking.",
                        Features = "[\"Intel Core i7 13700 / 32GB High-Speed RAM\",\"1TB NVMe Gen4 SSD + 2TB Enterprise HDD\",\"NVIDIA RTX Dedicated Graphics Support\",\"ISV Certified for Major Professional Applications\"]",
                        Specifications = "[\"Processor: Intel Core i7-13700K 16-Core\",\"Memory: 32GB Dual-Channel DDR5 5200MHz\",\"Storage: 1TB NVMe SSD + 2TB 7200RPM HDD\",\"Power: 750W 80-Plus Gold Modular PSU\",\"Ports: 8x USB 3.2, 2x Thunderbolt 4, Dual DisplayPort\",\"OS: Windows 11 Pro Workstation\"]",
                        Status = "ACTIVE",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Product
                    {
                        Name = "PowerEdge Enterprise Server X1",
                        CategoryId = serverCat,
                        Pricing = 150000,
                        Description = "2U rackmount high-availability server optimized for mission-critical databases, private cloud, and virtualization clusters.",
                        Features = "[\"Dual Intel Xeon Silver Scalable Processors\",\"64GB ECC Registered DDR4 Server Memory\",\"Hot-Swappable Redundant Titanium Power Supplies\",\"Hardware RAID Controller with 2GB Cache\"]",
                        Specifications = "[\"Form Factor: 2U Rackmount Chassis\",\"Processors: Dual Intel Xeon Silver 4314 (32 vCPUs)\",\"Memory: 64GB DDR4 3200MHz ECC Registered\",\"Bays: 8x 2.5-inch Hot-Plug SAS/SATA Bays\",\"Networking: 4x 10GbE SFP+ Ports\",\"Remote Management: Dedicated IPMI 2.0 / iDRAC\"]",
                        Status = "ACTIVE",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Product
                    {
                        Name = "Enterprise Core Network Router 10G",
                        CategoryId = networkCat,
                        Pricing = 35000,
                        Description = "High-throughput managed enterprise switch and router featuring hardware accelerated VPN, VLAN segregation, and L3 QoS.",
                        Features = "[\"10Gbps SFP+ Fiber Uplinks + 24x Gigabit PoE+ Ports\",\"Hardware-Accelerated IPsec & WireGuard VPN\",\"Layer 3 Switching & Dynamic BGP/OSPF Routing\",\"Redundant Dual Hot-Swap AC Power Inputs\"]",
                        Specifications = "[\"Throughput: Up to 128 Gbps Non-Blocking Wire Speed\",\"Ports: 24x 1GbE RJ45 PoE+, 4x 10G SFP+ Uplinks\",\"Power Budget: 370W Total PoE Allocation\",\"Security: Stateful Inspection Firewall, 802.1X, ACLs\",\"Form Factor: 1U Rackmount Kit Included\"]",
                        Status = "ACTIVE",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new Product
                    {
                        Name = "Hybrid Cloud Storage Gateway Node",
                        CategoryId = cloudCat,
                        Pricing = 85000,
                        Description = "Secure localized caching appliance syncing enterprise file shares directly to AWS S3, Azure Blob, and private object stores.",
                        Features = "[\"Real-time Automated Cloud Mirroring & Deduplication\",\"Encrypted Local High-Speed SSD Cache Tier\",\"Automated Disaster Recovery & Instant Failover\",\"Multi-Tenant Role-Based Access Control (RBAC)\"]",
                        Specifications = "[\"Storage: 4TB NVMe SSD Tier + 16TB Enterprise Archive\",\"Cloud Protocols: S3, Azure Blob, NFS v4, SMB 3.0\",\"Encryption: AES-256 at Rest and TLS 1.3 in Flight\",\"Bandwidth: Dual 10G Optical Interfaces\"]",
                        Status = "ACTIVE",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                });
                await context.SaveChangesAsync();
            }

            // Link existing products to matching categories if CategoryId is null
            var allCats = await context.Categories.ToListAsync();
            var laptopC = allCats.FirstOrDefault(c => c.CategoryName.Contains("Laptop"));
            var desktopC = allCats.FirstOrDefault(c => c.CategoryName.Contains("Desktop"));
            var serverC = allCats.FirstOrDefault(c => c.CategoryName.Contains("Server"));
            var netC = allCats.FirstOrDefault(c => c.CategoryName.Contains("Network") || c.CategoryName.Contains("Cloud"));

            var productsToUpdate = await context.Products.Where(p => p.CategoryId == null).ToListAsync();
            foreach (var p in productsToUpdate)
            {
                if (p.Name.Contains("Laptop", StringComparison.OrdinalIgnoreCase) && laptopC != null)
                    p.CategoryId = laptopC.CategoryId;
                else if (p.Name.Contains("Desktop", StringComparison.OrdinalIgnoreCase) && desktopC != null)
                    p.CategoryId = desktopC.CategoryId;
                else if (p.Name.Contains("Server", StringComparison.OrdinalIgnoreCase) && serverC != null)
                    p.CategoryId = serverC.CategoryId;
                else if (netC != null)
                    p.CategoryId = netC.CategoryId;
            }
            await context.SaveChangesAsync();

            // Seed Default Score Rules if empty
            if (!await context.ScoreRules.AnyAsync())
            {
                context.ScoreRules.AddRange(new[]
                {
                    new ScoreRule { Name = "Catalog Page View", EventType = "PAGE_VIEW", Category = "ENGAGEMENT", Direction = "ADD", Points = 5, IsActive = true, Description = "Visitor browsed general public pages" },
                    new ScoreRule { Name = "Product View", EventType = "PRODUCT_VIEW", Category = "ENGAGEMENT", Direction = "ADD", Points = 10, IsActive = true, Description = "Visitor viewed a specific product hardware detail page" },
                    new ScoreRule { Name = "Product Comparison", EventType = "PRODUCT_COMPARE", Category = "HIGH_INTENT", Direction = "ADD", Points = 20, IsActive = true, Description = "Visitor compared two or more hardware specs side-by-side" },
                    new ScoreRule { Name = "Click Interested", EventType = "INTEREST_CLICK", Category = "HIGH_INTENT", Direction = "ADD", Points = 30, IsActive = true, Description = "Visitor clicked I'm Interested CTA button" },
                    new ScoreRule { Name = "Submit Lead Form", EventType = "FORM_SUBMIT", Category = "HIGH_INTENT", Direction = "ADD", Points = 40, IsActive = true, Description = "Visitor submitted commercial inquiry or contact form" }
                });
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbInitializer] Seed error: {ex.Message}");
        }
    }
}
