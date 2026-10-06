using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Catalog.Infrastructure.Tests;

public sealed class CatalogModelTests
{
    [Fact]
    public void ProductModel_RequiresSkuAndHasUniqueSkuIndex()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(typeof(Product));
        Assert.NotNull(entityType);

        var sku = entityType.FindProperty(nameof(Product.Sku));
        Assert.NotNull(sku);
        Assert.False(sku.IsNullable);
        Assert.Equal(50, sku.GetMaxLength());

        var skuIndex = Assert.Single(entityType.GetIndexes());
        Assert.True(skuIndex.IsUnique);
        Assert.Equal(nameof(Product.Sku), Assert.Single(skuIndex.Properties).Name);
    }

    private static CatalogDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlServer("Server=(local);Database=CatalogModelTests;Integrated Security=True;TrustServerCertificate=True")
            .Options;

        return new CatalogDbContext(options);
    }
}
