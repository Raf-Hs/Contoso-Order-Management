using BuildingBlocks.Domain;
using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public sealed class Product : Entity<Guid>
{
    private Product()
    {
    }

    public Product(
        string sku,
        string name,
        string description,
        decimal price,
        int availableQuantity) : base(Guid.NewGuid())
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("SKU is required.", nameof(sku));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.", nameof(name));

            ArgumentNullException.ThrowIfNull(description);

            if (price < 0)
                throw new ArgumentException("Price cannot be negative.", nameof(price));

            if (availableQuantity < 0)
                throw new ArgumentException(
                    "Available quantity cannot be negative.",
                    nameof(availableQuantity));

            Sku = sku;
            Name = name;
            Description = description;
            Price = price;
            AvailableQuantity = availableQuantity;
        }

    public string Sku { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public decimal Price { get; private set; }

    public int AvailableQuantity { get; private set; }

    public void UpdateDetails(
        string sku,
        string name,
        string description,
        decimal price)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.", nameof(sku));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        ArgumentNullException.ThrowIfNull(description);

        if (price < 0)
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Price cannot be negative.");

        Sku = sku;
        Name = name;
        Description = description;
        Price = price;
    }

    public bool IsAvailable(int quantity)
    {
        return quantity > 0 &&
               AvailableQuantity >= quantity;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException(
                "Quantity must be greater than zero.",
                nameof(quantity));

        AvailableQuantity += quantity;
    }

    public void DecreaseStock(int quantity)
    {
        if (!IsAvailable(quantity))
            throw new InsufficientInventoryException(
                "Insufficient inventory.");

        AvailableQuantity -= quantity;
    }
}
