using Orders.Domain.Entities;
using Xunit;

namespace Orders.Domain.Tests;

public sealed class OrderItemTests
{
    [Fact]
    public void Constructor_WithEmptyProductId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new OrderItem(Guid.Empty, "Widget", 10m, 1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingProductName_ThrowsArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(
            () => new OrderItem(Guid.NewGuid(), name!, 10m, 1));
    }

    [Fact]
    public void Constructor_WithNegativeUnitPrice_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OrderItem(Guid.NewGuid(), "Widget", -0.01m, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveQuantity_ThrowsArgumentException(int quantity)
    {
        Assert.Throws<ArgumentException>(
            () => new OrderItem(Guid.NewGuid(), "Widget", 10m, quantity));
    }

    [Fact]
    public void Total_IsUnitPriceTimesQuantity()
    {
        var item = new OrderItem(Guid.NewGuid(), "Widget", 12.50m, 3);

        Assert.Equal(37.50m, item.Total);
    }

    [Fact]
    public void Constructor_AllowsZeroUnitPrice()
    {
        var item = new OrderItem(Guid.NewGuid(), "Promotional item", 0m, 1);

        Assert.Equal(0m, item.Total);
    }
}
