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
                "Product ID is required.",
                nameof(productId));

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException(
                "Product name is required.",
                nameof(productName));

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                "Unit price cannot be negative.");

        if (quantity <= 0)
            throw new ArgumentException(
                "Quantity must be greater than zero.",
                nameof(quantity));

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
