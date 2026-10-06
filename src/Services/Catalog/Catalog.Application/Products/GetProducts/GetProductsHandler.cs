namespace Catalog.Application.Products.GetProducts;

public sealed class GetProductsHandler(IProductRepository repository)
{
    public async Task<IReadOnlyCollection<ProductDto>> HandleAsync(
        GetProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var products = await repository.GetAllAsync(cancellationToken);
        return products.Select(ProductDtoMapper.Map).ToArray();
    }
}
