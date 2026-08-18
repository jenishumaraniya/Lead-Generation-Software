using CrmLeadTool.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CrmLeadTool.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<VisitorActivity> VisitorActivities => Set<VisitorActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Unique indexes
        modelBuilder.Entity<Visitor>()
            .HasIndex(v => v.AnonymousId)
            .IsUnique();

        modelBuilder.Entity<Visitor>()
            .HasIndex(v => v.PublicId)
            .IsUnique();

        // Product → Category (foreign key)
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // VisitorActivity → Visitor (CASCADE)
        modelBuilder.Entity<VisitorActivity>()
            .HasOne(a => a.Visitor)
            .WithMany(v => v.Activities)
            .HasForeignKey(a => a.VisitorId)
            .OnDelete(DeleteBehavior.Cascade);   // matches your SQL

        // VisitorActivity → Product (SET NULL)
        modelBuilder.Entity<VisitorActivity>()
            .HasOne(a => a.Product)
            .WithMany(p => p.Activities)
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        // Optional: configure indexes (already in SQL, but can be defined here)
        modelBuilder.Entity<VisitorActivity>()
            .HasIndex(a => a.VisitorId);

        modelBuilder.Entity<VisitorActivity>()
            .HasIndex(a => a.ProductId);

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.CategoryId);
    }
}