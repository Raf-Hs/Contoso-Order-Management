namespace Catalog.Application.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string Description,
    decimal Price,
    int AvailableQuantity);
