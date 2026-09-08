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
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure();
            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
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

// Ensure uploads directory exists and configure static files
var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var uploadsDir = Path.Combine(webRoot, "uploads");
if (!Directory.Exists(uploadsDir))
{
    Directory.CreateDirectory(uploadsDir);
}

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles(); // Default wwwroot
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsDir),
    RequestPath = "/uploads"
});

app.UseCors("Frontend");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.MapControllers();

app.Run();