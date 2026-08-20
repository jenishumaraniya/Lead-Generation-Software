using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Services;
using CrmLeadTool.Api.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Sprint 1 services
builder.Services.AddScoped<VisitorService>();

// Sprint 2 services
builder.Services.AddScoped<ProspectService>();
builder.Services.AddScoped<CampaignService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<TrackingService>();

// Background worker for scheduled email sending
builder.Services.AddHostedService<EmailSchedulerWorker>();

// CORS policy (allow any origin for development)
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

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");

app.UseHttpsRedirection();

app.MapControllers();

app.Run();