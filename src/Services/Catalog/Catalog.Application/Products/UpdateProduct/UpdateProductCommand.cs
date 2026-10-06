namespace Catalog.Application.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Sku,
    string Name,
    string Description,
    decimal Price);
