namespace Orders.Application.Orders;

public sealed record CatalogProduct(
    Guid Id,
    string Name,
    decimal Price,
    int AvailableQuantity);
