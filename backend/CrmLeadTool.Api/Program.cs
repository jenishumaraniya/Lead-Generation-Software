using CrmLeadTool.Api.Data;
using CrmLeadTool.Api.Services;
using CrmLeadTool.Api.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

// Force 100% Pure Managed Networking on Windows (bypasses native SNI and C++ dependencies)
AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);

var builder = WebApplication.CreateBuilder(args);

// Dynamically locate wwwroot containing index.html across IIS/Kestrel environments
string[] candidateRoots = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
    Directory.GetCurrentDirectory(),
    Path.Combine(AppContext.BaseDirectory, "wwwroot"),
    AppContext.BaseDirectory
};
string webRoot = candidateRoots.FirstOrDefault(dir => File.Exists(Path.Combine(dir, "index.html"))) 
                 ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

builder.Environment.WebRootPath = webRoot;

// Suppress Server Header & Enforce 10MB Body Limit on Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 25 * 1024 * 1024; // 25 MB limit for product image uploads
});

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
builder.Services.AddScoped<NotificationService>();

// HttpClient for External Providers
builder.Services.AddHttpClient();
builder.Services.AddScoped<GroqAIService>();

// Background Workers
builder.Services.AddHostedService<EmailSchedulerWorker>();

// Rate Limiting (DDoS & Brute Force Defense)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // General API rate limit (100 requests per minute per IP)
    options.AddPolicy("GeneralPolicy", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 5
            }));

    // Strict Auth rate limit (5 attempts per minute per IP for login/auth endpoints)
    options.AddPolicy("AuthPolicy", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 3,
                QueueLimit = 0
            }));
});

// JWT Authentication Configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SUPER_SECRET_KEY_FOR_B2B_LEAD_GENERATION_PLATFORM_2026_CRM_TOKEN_AUTH!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CrmLeadToolApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CrmLeadToolWeb";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();

// CORS Hardening
var allowedOriginsConfig = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:4200", "http://localhost:5234" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return true;
            // Chrome extension origin
            if (origin.StartsWith("chrome-extension://", StringComparison.OrdinalIgnoreCase)) return true;
            // LinkedIn pages
            if (origin.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase)) return true;
            // Localhost / loopback
            if (origin.Contains("localhost", StringComparison.OrdinalIgnoreCase) || origin.Contains("127.0.0.1")) return true;
            // Any live origins configured or incoming web origins
            return true;
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure Database Tables
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(db);
}

// Global Exception Shielding Middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Unhandled exception occurred on request {Path}", context.Request.Path);

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var isDev = app.Environment.IsDevelopment();
            var response = new
            {
                success = false,
                message = isDev ? ex.Message : "An error occurred while processing your request. Please try again later.",
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
});

// Security Headers & Signature Stripping Middleware
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        // Remove revealing server headers
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");
        context.Response.Headers.Remove("X-AspNetMvc-Version");

        // Inject industry-standard security headers
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        return Task.CompletedTask;
    });

    await next();
});

// Configure Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseHttpsRedirection();

// Serve static files from all candidate roots (root and wwwroot subfolder)
app.UseDefaultFiles();
app.UseStaticFiles();

var curDir = Directory.GetCurrentDirectory();
var subWww = Path.Combine(curDir, "wwwroot");
var baseDir = AppContext.BaseDirectory;
var baseSubWww = Path.Combine(baseDir, "wwwroot");
var uploadsDir = Path.Combine(subWww, "uploads");

if (!Directory.Exists(uploadsDir))
{
    Directory.CreateDirectory(uploadsDir);
}
var productsDir = Path.Combine(uploadsDir, "products");
if (!Directory.Exists(productsDir))
{
    Directory.CreateDirectory(productsDir);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsDir),
    RequestPath = "/uploads"
});

foreach (var dir in new[] { subWww, curDir, baseSubWww, baseDir }.Distinct())
{
    if (Directory.Exists(dir))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(dir)
        });
    }
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/health", () =>
{
    var candidates = new[]
    {
        Path.Combine(subWww, "index.html"),
        Path.Combine(curDir, "index.html"),
        Path.Combine(baseSubWww, "index.html"),
        Path.Combine(baseDir, "index.html")
    };
    var found = candidates.FirstOrDefault(File.Exists);
    return Results.Ok(new
    {
        status = "Healthy",
        currentDir = curDir,
        baseDir = baseDir,
        matchedIndexPath = found,
        timestamp = DateTime.UtcNow
    });
});

app.MapGet("/api/db-check", async (AppDbContext db) =>
{
    try
    {
        // Auto-provision any missing tables & seeds
        await DbInitializer.InitializeAsync(db);
        var canConnect = await db.Database.CanConnectAsync();
        var productCount = await db.Products.CountAsync();
        var categoryCount = await db.Categories.CountAsync();
        return Results.Ok(new
        {
            success = true,
            canConnect,
            productCount,
            categoryCount,
            message = "Database connected and fully operational!"
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            success = false,
            error = ex.Message,
            inner = ex.InnerException?.Message
        });
    }
});

// Direct Favicon Endpoint (Guarantees correct image/x-icon MIME type and prevents index.html fallback)
app.MapGet("/favicon.ico", async context =>
{
    var candidates = new[]
    {
        Path.Combine(subWww, "favicon.ico"),
        Path.Combine(curDir, "favicon.ico"),
        Path.Combine(baseSubWww, "favicon.ico"),
        Path.Combine(baseDir, "favicon.ico"),
        Path.Combine(subWww, "assets", "favicon.ico"),
        Path.Combine(curDir, "assets", "favicon.ico")
    };
    var matched = candidates.FirstOrDefault(File.Exists);
    if (matched != null)
    {
        context.Response.ContentType = "image/x-icon";
        context.Response.Headers.CacheControl = "public, max-age=604800";
        await context.Response.SendFileAsync(matched);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }
});

// Explicit routes for branded favicon PNGs
app.MapGet("/assets/{filename}", async (string filename, HttpContext context) =>
{
    if (filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
        filename.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
        filename.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
    {
        var candidates = new[]
        {
            Path.Combine(subWww, "assets", filename),
            Path.Combine(curDir, "assets", filename),
            Path.Combine(baseSubWww, "assets", filename),
            Path.Combine(baseDir, "assets", filename)
        };
        var matched = candidates.FirstOrDefault(File.Exists);
        if (matched != null)
        {
            var contentType = filename.EndsWith(".png") ? "image/png" :
                              filename.EndsWith(".ico") ? "image/x-icon" : "image/svg+xml";
            context.Response.ContentType = contentType;
            context.Response.Headers.CacheControl = "public, max-age=604800";
            await context.Response.SendFileAsync(matched);
            return;
        }
    }
    context.Response.StatusCode = StatusCodes.Status404NotFound;
});

app.MapFallback(async context =>
{
    var path = context.Request.Path.Value ?? "";
    // If the request targets a static asset with a file extension, do NOT serve index.html
    if (Path.HasExtension(path) && !path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var candidates = new[]
    {
        Path.Combine(subWww, "index.html"),
        Path.Combine(curDir, "index.html"),
        Path.Combine(baseSubWww, "index.html"),
        Path.Combine(baseDir, "index.html")
    };

    var matched = candidates.FirstOrDefault(File.Exists);
    if (matched != null)
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(matched);
    }
    else
    {
        context.Response.StatusCode = 404;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("LeadFlow CRM: index.html not found. Checked:\n" + string.Join("\n", candidates));
    }
});

app.Run();