using BuildingBlocks.Domain;
using Orders.Domain.Enums;
using Orders.Domain.Exceptions;

namespace Orders.Domain.Entities;

public sealed class Order : Entity<Guid>
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    public Order(
        Guid customerId,
        Guid createdBy)
        : base(Guid.NewGuid())
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException(
                "Customer ID is required.");

        if (createdBy == Guid.Empty)
            throw new ArgumentException(
                "CreatedBy is required.");

        CustomerId = customerId;
        CreatedBy = createdBy;
        Status = OrderStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }

    public Guid CreatedBy { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal Total => _items.Sum(x => x.Total);

    public void AddItem(
        Guid productId,
        string productName,
        decimal unitPrice,
        int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new OrderStateException(
                "Items can only be added to draft orders.");

        _items.Add(
            new OrderItem(
                productId,
                productName,
                unitPrice,
                quantity));
    }

    public void Submit()
    {
        if (_items.Count == 0)
            throw new OrderStateException(
                "Order must contain at least one item.");

        if (Status != OrderStatus.Draft)
            throw new OrderStateException(
                "Only draft orders can be submitted.");

        Status = OrderStatus.PendingApproval;
    }

    public void Approve()
    {
        if (Status != OrderStatus.PendingApproval)
            throw new OrderStateException(
                "Only pending approval orders can be approved.");

        Status = OrderStatus.Approved;
    }

    public void Reject()
    {
        if (Status != OrderStatus.PendingApproval)
            throw new OrderStateException(
                "Only pending approval orders can be rejected.");

        Status = OrderStatus.Rejected;
    }

    public void Cancel()
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.PendingApproval or OrderStatus.Approved))
            throw new OrderStateException(
                "Only draft, pending approval, or approved orders can be cancelled.");

        Status = OrderStatus.Cancelled;
    }

    public void StartPreparing()
    {
        if (Status != OrderStatus.Approved)
            throw new OrderStateException(
                "Only approved orders can start preparation.");

        Status = OrderStatus.Preparing;
    }
}
