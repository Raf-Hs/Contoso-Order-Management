using BuildingBlocks.Domain;

namespace Orders.Domain.Entities;

public sealed class OrderItem : Entity<Guid>
{
    private OrderItem()
    {
    }

    public OrderItem(
        Guid productId,
        string productName,
        decimal unitPrice,
        int quantity)
        : base(Guid.NewGuid())
    {
        if (productId == Guid.Empty)
            throw new ArgumentException(
                "Product ID is required.");

        if (quantity <= 0)
            throw new ArgumentException(
                "Quantity must be greater than zero.");

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = null!;

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public decimal Total => UnitPrice * Quantity;
}