namespace Catalog.Application.Products;

public sealed class ProductNotFoundException(Guid productId)
    : Exception($"Product '{productId}' was not found.");
