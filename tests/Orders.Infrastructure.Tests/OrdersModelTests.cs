using Microsoft.EntityFrameworkCore;
using Orders.Domain.Entities;
using Orders.Infrastructure.Persistence;
using Xunit;

namespace Orders.Infrastructure.Tests;

public sealed class OrdersModelTests
{
    [Fact]
    public void OrderItem_HasOneRequiredCascadeRelationshipToOrder()
    {
        using var context = CreateContext();

        var itemType = context.Model.FindEntityType(typeof(OrderItem));
        Assert.NotNull(itemType);

        var relationship = Assert.Single(itemType.GetForeignKeys());
        Assert.Equal(typeof(Order), relationship.PrincipalEntityType.ClrType);
        Assert.True(relationship.IsRequired);
        Assert.Equal(DeleteBehavior.Cascade, relationship.DeleteBehavior);
        Assert.Equal("OrderId", Assert.Single(relationship.Properties).Name);
        Assert.Null(itemType.FindProperty("OrderId1"));
    }

    [Fact]
    public void CreateScript_ContainsOnlyTheExpectedOrderForeignKey()
    {
        using var context = CreateContext();

        var script = context.Database.GenerateCreateScript();

        Assert.Contains("[OrderId]", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OrderId1", script, StringComparison.OrdinalIgnoreCase);
    }

    private static OrdersDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlServer("Server=(local);Database=OrdersModelTests;Integrated Security=True;TrustServerCertificate=True")
            .Options;

        return new OrdersDbContext(options);
    }
}
