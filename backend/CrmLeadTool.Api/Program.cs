using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Services;
using CrmLeadTool.Api.Workers;
using Microsoft.EntityFrameworkCore;

// Pre-create wwwroot and uploads directory before host builder initializes static web assets
var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var uploadsPath = Path.Combine(webRootPath, "uploads");
if (!Directory.Exists(webRootPath)) Directory.CreateDirectory(webRootPath);
if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions =>
        {
            sqlServerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            sqlServerOptions.EnableRetryOnFailure();
        }));

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

// HttpClient for External Providers (IHttpClientFactory available to all scoped services)
builder.Services.AddHttpClient(); // registers IHttpClientFactory
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

app.UseStaticFiles(); // Serves files from wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.UseCors("Frontend");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.MapControllers();

app.Run();