using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Services;
using CrmLeadTool.Api.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Sprint 1
builder.Services.AddScoped<VisitorService>();

// Sprint 2
builder.Services.AddScoped<ProspectService>();
builder.Services.AddScoped<CampaignService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<TrackingService>();
builder.Services.AddHostedService<EmailSchedulerWorker>();

// Sprint 3 (NEW)
builder.Services.AddScoped<DuplicateService>();
builder.Services.AddScoped<LeadService>();

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

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");
app.UseHttpsRedirection();
app.MapControllers();

app.Run();