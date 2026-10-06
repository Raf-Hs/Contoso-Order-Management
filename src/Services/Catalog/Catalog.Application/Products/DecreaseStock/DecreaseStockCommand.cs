namespace Catalog.Application.Products.DecreaseStock;

public sealed record DecreaseStockCommand(Guid ProductId, int Quantity);
