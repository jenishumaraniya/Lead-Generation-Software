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
                  LastLoginAt DATETIME2 NULL
              )",

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

            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ScoreRule_CRM')
              CREATE TABLE ScoreRule_CRM (
                  ScoreRuleId INT IDENTITY(1,1) PRIMARY KEY,
                  Name NVARCHAR(255) NOT NULL,
                  EventType NVARCHAR(100) NOT NULL,
                  Category NVARCHAR(100) NOT NULL,
                  Direction NVARCHAR(50) NOT NULL,
                  Points INT NOT NULL,
                  IsActive BIT NOT NULL,
                  Description NVARCHAR(500) NULL,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                  UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

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

            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LeadNote_CRM')
              CREATE TABLE LeadNote_CRM (
                  LeadNoteId INT IDENTITY(1,1) PRIMARY KEY,
                  LeadId INT NOT NULL,
                  NoteText NVARCHAR(MAX) NOT NULL,
                  CreatedBy NVARCHAR(255) NULL,
                  CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
              )",

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

            @"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LeadActivity_CRM')
              CREATE TABLE LeadActivity_CRM (
                  LeadActivityId INT IDENTITY(1,1) PRIMARY KEY,
                  LeadId INT NOT NULL,
                  ActivityType NVARCHAR(100) NOT NULL,
                  Description NVARCHAR(MAX) NULL,
                  Metadata NVARCHAR(MAX) NULL,
                  CreatedBy NVARCHAR(255) NULL,
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
            // Lead_CRM: AssignedTo, Notes, NextFollowUpDate, IsMultiCategory
            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Lead_CRM' AND COLUMN_NAME='AssignedTo')
              ALTER TABLE Lead_CRM ADD AssignedTo INT NULL",

            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Lead_CRM' AND COLUMN_NAME='Notes')
              ALTER TABLE Lead_CRM ADD Notes NVARCHAR(MAX) NULL",

            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Lead_CRM' AND COLUMN_NAME='NextFollowUpDate')
              ALTER TABLE Lead_CRM ADD NextFollowUpDate DATETIME2 NULL",

            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Lead_CRM' AND COLUMN_NAME='IsMultiCategory')
              ALTER TABLE Lead_CRM ADD IsMultiCategory BIT NOT NULL DEFAULT 0",

            // User_CRM: CategoryId for salesperson-category assignment (if not exists)
            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='User_CRM' AND COLUMN_NAME='CategoryId')
              ALTER TABLE User_CRM ADD CategoryId INT NULL",

            // Campaign_CRM: ScheduleStartDate and ScheduleEndDate
            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Campaign_CRM' AND COLUMN_NAME='ScheduleStartDate')
              ALTER TABLE Campaign_CRM ADD ScheduleStartDate DATETIME2 NULL",

            @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Campaign_CRM' AND COLUMN_NAME='ScheduleEndDate')
              ALTER TABLE Campaign_CRM ADD ScheduleEndDate DATETIME2 NULL",
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

        // Seed Default Users if empty
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

            // Ensure all existing users are active and unlocked
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
            var defaultCategoryNames = new[] { "Laptops", "Desktops", "Servers", "Networking", "Cloud Solutions" };
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

            // Link existing products to matching categories if CategoryId is null
            var allCats = await context.Categories.ToListAsync();
            var laptopCat = allCats.FirstOrDefault(c => c.CategoryName.Contains("Laptop"));
            var desktopCat = allCats.FirstOrDefault(c => c.CategoryName.Contains("Desktop"));
            var serverCat = allCats.FirstOrDefault(c => c.CategoryName.Contains("Server"));
            var netCat = allCats.FirstOrDefault(c => c.CategoryName.Contains("Network") || c.CategoryName.Contains("Cloud"));

            var productsToUpdate = await context.Products.Where(p => p.CategoryId == null).ToListAsync();
            foreach (var p in productsToUpdate)
            {
                if (p.Name.Contains("Laptop", StringComparison.OrdinalIgnoreCase) && laptopCat != null)
                    p.CategoryId = laptopCat.CategoryId;
                else if (p.Name.Contains("Desktop", StringComparison.OrdinalIgnoreCase) && desktopCat != null)
                    p.CategoryId = desktopCat.CategoryId;
                else if (p.Name.Contains("Server", StringComparison.OrdinalIgnoreCase) && serverCat != null)
                    p.CategoryId = serverCat.CategoryId;
                else if (netCat != null)
                    p.CategoryId = netCat.CategoryId;
            }
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DbInitializer] User/Category seed error: {ex.Message}");
        }
    }
}
