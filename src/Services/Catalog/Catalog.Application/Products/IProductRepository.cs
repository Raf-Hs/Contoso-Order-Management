using Catalog.Domain.Entities;

namespace Catalog.Application.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Product>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<bool> SkuExistsAsync(
        string sku,
        Guid? excludingProductId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
