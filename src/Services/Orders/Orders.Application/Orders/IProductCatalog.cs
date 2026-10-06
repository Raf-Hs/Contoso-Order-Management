namespace Orders.Application.Orders;

public interface IProductCatalog
{
    Task<CatalogProduct?> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
}
