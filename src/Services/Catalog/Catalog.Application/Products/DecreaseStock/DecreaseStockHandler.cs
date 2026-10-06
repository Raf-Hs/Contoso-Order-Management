namespace Catalog.Application.Products.DecreaseStock;

public sealed class DecreaseStockHandler(IProductRepository repository)
{
    public async Task<ProductDto> HandleAsync(
        DecreaseStockCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await repository.GetByIdAsync(command.ProductId, cancellationToken)
            ?? throw new ProductNotFoundException(command.ProductId);

        product.DecreaseStock(command.Quantity);
        await repository.SaveChangesAsync(cancellationToken);
        return ProductDtoMapper.Map(product);
    }
}
