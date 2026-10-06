using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Xunit;

namespace Catalog.Domain.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesProduct()
    {
        var product = new Product("SKU-1", "Widget", "Description", 12.50m, 4);

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("SKU-1", product.Sku);
        Assert.Equal(12.50m, product.Price);
        Assert.Equal(4, product.AvailableQuantity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingSku_ThrowsArgumentException(string? sku)
    {
        Assert.Throws<ArgumentException>(
            () => new Product(sku!, "Widget", "Description", 1m, 0));
    }

    [Fact]
    public void Constructor_WithMissingName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new Product("SKU-1", " ", "Description", 1m, 0));
    }

    [Fact]
    public void Constructor_WithNegativePrice_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new Product("SKU-1", "Widget", "Description", -1m, 0));
    }

    [Fact]
    public void Constructor_WithNegativeStock_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new Product("SKU-1", "Widget", "Description", 1m, -1));
    }

    [Fact]
    public void IsAvailable_RequiresPositiveQuantityWithinStock()
    {
        var product = new Product("SKU-1", "Widget", "Description", 1m, 2);

        Assert.True(product.IsAvailable(2));
        Assert.False(product.IsAvailable(0));
        Assert.False(product.IsAvailable(3));
    }

    [Fact]
    public void IncreaseStock_WithPositiveQuantity_IncreasesAvailableQuantity()
    {
        var product = new Product("SKU-1", "Widget", "Description", 1m, 2);

        product.IncreaseStock(3);

        Assert.Equal(5, product.AvailableQuantity);
    }

    [Fact]
    public void IncreaseStock_WithNonPositiveQuantity_ThrowsArgumentException()
    {
        var product = new Product("SKU-1", "Widget", "Description", 1m, 2);

        Assert.Throws<ArgumentException>(() => product.IncreaseStock(0));
    }

    [Fact]
    public void DecreaseStock_WithSufficientStock_DecreasesAvailableQuantity()
    {
        var product = new Product("SKU-1", "Widget", "Description", 1m, 3);

        product.DecreaseStock(2);

        Assert.Equal(1, product.AvailableQuantity);
    }

    [Fact]
    public void DecreaseStock_WithInsufficientStock_ThrowsDomainException()
    {
        var product = new Product("SKU-1", "Widget", "Description", 1m, 1);

        Assert.Throws<InsufficientInventoryException>(() => product.DecreaseStock(2));
    }

    [Fact]
    public void UpdateDetails_ChangesProductInformation()
    {
        var product = new Product("SKU-1", "Widget", "Old", 1m, 4);

        product.UpdateDetails("SKU-2", "New name", "New description", 7.25m);

        Assert.Equal("SKU-2", product.Sku);
        Assert.Equal("New name", product.Name);
        Assert.Equal("New description", product.Description);
        Assert.Equal(7.25m, product.Price);
        Assert.Equal(4, product.AvailableQuantity);
    }
}
