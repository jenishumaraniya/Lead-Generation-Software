using CrmLeadTool.Api.Data; 

using CrmLeadTool.Api.Services; 

using Microsoft.EntityFrameworkCore; 

var builder = WebApplication.CreateBuilder(args); 
 
builder.Services.AddControllers(); 


builder.Services.AddDbContext<AppDbContext>( 

    options => 

        options.UseSqlServer( 

            builder.Configuration 

              .GetConnectionString("DefaultConnection") 
        ) 
); 

builder.Services.AddScoped<VisitorService>(); 

builder.Services.AddCors(options => 

{ 

    options.AddPolicy("Frontend", policy => 

    { 
        policy 

            .AllowAnyOrigin() 

            .AllowAnyHeader() 

            .AllowAnyMethod(); 
    }); 
}); 

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(); 

var app = builder.Build(); 

app.UseSwagger(); 

app.UseSwaggerUI(); 

app.UseCors("Frontend"); 

app.UseHttpsRedirection(); 

app.MapControllers(); 

app.Run();