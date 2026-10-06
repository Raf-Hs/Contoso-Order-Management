namespace Catalog.Application.Products.GetProduct;

public sealed class GetProductHandler(IProductRepository repository)
{
    public async Task<ProductDto> HandleAsync(
        GetProductQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var product = await repository.GetByIdAsync(query.ProductId, cancellationToken)
            ?? throw new ProductNotFoundException(query.ProductId);

        return ProductDtoMapper.Map(product);
    }
}
