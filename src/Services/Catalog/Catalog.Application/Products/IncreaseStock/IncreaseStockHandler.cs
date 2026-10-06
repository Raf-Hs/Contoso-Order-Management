namespace Catalog.Application.Products.IncreaseStock;

public sealed class IncreaseStockHandler(IProductRepository repository)
{
    public async Task<ProductDto> HandleAsync(
        IncreaseStockCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await repository.GetByIdAsync(command.ProductId, cancellationToken)
            ?? throw new ProductNotFoundException(command.ProductId);

        product.IncreaseStock(command.Quantity);
        await repository.SaveChangesAsync(cancellationToken);
        return ProductDtoMapper.Map(product);
    }
}
