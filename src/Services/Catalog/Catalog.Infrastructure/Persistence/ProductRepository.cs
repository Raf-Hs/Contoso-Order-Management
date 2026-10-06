using Catalog.Application.Products;
using Catalog.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public sealed class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _context;

    public ProductRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<Product>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> SkuExistsAsync(
        string sku,
        Guid? excludingProductId = null,
        CancellationToken cancellationToken = default)
    {
        return _context.Products
            .AnyAsync(
                product => product.Sku == sku
                    && (!excludingProductId.HasValue || product.Id != excludingProductId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(
            product,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            var sku = _context.ChangeTracker.Entries<Product>()
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
                .Select(entry => entry.Entity.Sku)
                .FirstOrDefault() ?? "unknown";

            throw new DuplicateProductSkuException(sku, exception);
        }
    }
}
