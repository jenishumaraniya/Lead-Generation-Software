using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Services;
using CrmLeadTool.Api.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Domain & Application Services
builder.Services.AddScoped<VisitorService>();
builder.Services.AddScoped<DuplicateService>();
builder.Services.AddScoped<ScoringService>();
builder.Services.AddScoped<QualificationService>();
builder.Services.AddScoped<CompanyEnrichmentService>();
builder.Services.AddScoped<LinkedInEnrichmentService>();
builder.Services.AddScoped<ProspectDiscoveryService>();
builder.Services.AddScoped<ProspectService>();
builder.Services.AddScoped<CampaignService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<TrackingService>();
builder.Services.AddScoped<LeadService>();
builder.Services.AddScoped<HandoffService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<AuthService>();

// HttpClient for External Providers
builder.Services.AddHttpClient<EmailService>();
builder.Services.AddScoped<GroqAIService>();
builder.Services.AddHttpClient<GroqAIService>();

// Background Workers
builder.Services.AddHostedService<EmailSchedulerWorker>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Ensure Database Tables
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(db);
}

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();