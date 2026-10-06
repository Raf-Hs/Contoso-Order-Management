namespace Catalog.Application.Products.IncreaseStock;

public sealed record IncreaseStockCommand(Guid ProductId, int Quantity);
