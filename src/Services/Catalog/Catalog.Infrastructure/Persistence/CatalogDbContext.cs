using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext : DbContext
{
    public CatalogDbContext(
        DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Sku)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.Sku)
                .IsUnique();

            entity.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.Price)
                .HasPrecision(18, 2);

            entity.Property(x => x.AvailableQuantity)
                .IsRequired();
        });
    }
}