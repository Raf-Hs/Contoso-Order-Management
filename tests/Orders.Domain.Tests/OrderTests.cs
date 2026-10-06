using Orders.Domain.Entities;
using Orders.Domain.Enums;
using Orders.Domain.Exceptions;
using Xunit;

namespace Orders.Domain.Tests;

public sealed class OrderTests
{
    [Fact]
    public void Constructor_WithValidIds_CreatesDraftOrder()
    {
        var customerId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var order = new Order(customerId, createdBy);

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(createdBy, order.CreatedBy);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Empty(order.Items);
    }

    [Fact]
    public void Constructor_WithEmptyCustomerId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Order(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_WithEmptyCreatedBy_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Order(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void AddItem_WhenDraft_AddsItemAndUpdatesTotal()
    {
        var order = CreateOrder();
        var productId = Guid.NewGuid();

        order.AddItem(productId, "Widget", 12.50m, 2);

        var item = Assert.Single(order.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(25.00m, order.Total);
    }

    [Fact]
    public void AddItem_AfterSubmit_ThrowsOrderStateException()
    {
        var order = CreateSubmittedOrder();

        Assert.Throws<OrderStateException>(
            () => order.AddItem(Guid.NewGuid(), "Widget", 1m, 1));
    }

    [Fact]
    public void Submit_WithoutItems_ThrowsOrderStateException()
    {
        var order = CreateOrder();

        Assert.Throws<OrderStateException>(order.Submit);
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public void Submit_WithItems_ChangesStatusToPendingApproval()
    {
        var order = CreateOrderWithItem();

        order.Submit();

        Assert.Equal(OrderStatus.PendingApproval, order.Status);
    }

    [Fact]
    public void Approve_WhenNotPendingApproval_ThrowsOrderStateException()
    {
        var order = CreateOrderWithItem();

        Assert.Throws<OrderStateException>(order.Approve);
    }

    [Fact]
    public void Approve_WhenPendingApproval_ChangesStatusToApproved()
    {
        var order = CreateSubmittedOrder();

        order.Approve();

        Assert.Equal(OrderStatus.Approved, order.Status);
    }

    [Fact]
    public void Reject_WhenNotPendingApproval_ThrowsOrderStateException()
    {
        var order = CreateOrderWithItem();

        Assert.Throws<OrderStateException>(order.Reject);
    }

    [Fact]
    public void Reject_WhenPendingApproval_ChangesStatusToRejected()
    {
        var order = CreateSubmittedOrder();

        order.Reject();

        Assert.Equal(OrderStatus.Rejected, order.Status);
    }

    [Fact]
    public void Cancel_WhenPendingApproval_ChangesStatusToCancelled()
    {
        var order = CreateSubmittedOrder();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_WhenPreparing_ThrowsOrderStateException()
    {
        var order = CreateSubmittedOrder();
        order.Approve();
        order.StartPreparing();

        Assert.Throws<OrderStateException>(order.Cancel);
    }

    [Fact]
    public void StartPreparing_WhenNotApproved_ThrowsOrderStateException()
    {
        var order = CreateSubmittedOrder();

        Assert.Throws<OrderStateException>(order.StartPreparing);
    }

    [Fact]
    public void StartPreparing_WhenApproved_ChangesStatusToPreparing()
    {
        var order = CreateSubmittedOrder();
        order.Approve();

        order.StartPreparing();

        Assert.Equal(OrderStatus.Preparing, order.Status);
    }

    private static Order CreateOrder() => new(Guid.NewGuid(), Guid.NewGuid());

    private static Order CreateOrderWithItem()
    {
        var order = CreateOrder();
        order.AddItem(Guid.NewGuid(), "Widget", 10m, 1);
        return order;
    }

    private static Order CreateSubmittedOrder()
    {
        var order = CreateOrderWithItem();
        order.Submit();
        return order;
    }
}
