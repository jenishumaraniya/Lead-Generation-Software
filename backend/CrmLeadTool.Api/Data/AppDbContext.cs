using CrmLeadTool.Api.Models; 
using Microsoft.EntityFrameworkCore; 
namespace CrmLeadTool.Api.Data; 
public class AppDbContext : DbContext 
{ 
    public AppDbContext( 
        DbContextOptions<AppDbContext> options) 
        : base(options)
    {
        
    } 
    public DbSet<Visitor> Visitors => Set<Visitor>(); 
    public DbSet<VisitorActivity> VisitorActivities 
        => Set<VisitorActivity>();  

    public DbSet<Product> Products 
        => Set<Product>(); 
    protected override void OnModelCreating( 
        ModelBuilder modelBuilder) 
    { 
        modelBuilder.Entity<Visitor>() 
            .HasIndex(v => v.AnonymousId) 
            .IsUnique(); 

        modelBuilder.Entity<VisitorActivity>() 
            .HasOne(a => a.Visitor) 
            .WithMany(v => v.Activities) 
            .HasForeignKey(a => a.VisitorId); 

        modelBuilder.Entity<VisitorActivity>() 
            .HasOne(a => a.Product) 
            .WithMany(p => p.Activities) 
            .HasForeignKey(a => a.ProductId) 
            .OnDelete(DeleteBehavior.SetNull); 
    } 
} 