namespace Orders.Application.Orders;

public sealed class CatalogProductNotFoundException(Guid productId)
    : Exception($"Product '{productId}' was not found in Catalog.");
