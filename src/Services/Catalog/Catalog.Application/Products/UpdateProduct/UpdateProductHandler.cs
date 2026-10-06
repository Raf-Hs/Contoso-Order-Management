namespace Catalog.Application.Products.UpdateProduct;

public sealed class UpdateProductHandler(IProductRepository repository)
{
    public async Task<ProductDto> HandleAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await repository.GetByIdAsync(command.ProductId, cancellationToken)
            ?? throw new ProductNotFoundException(command.ProductId);

        if (await repository.SkuExistsAsync(
                command.Sku,
                command.ProductId,
                cancellationToken))
        {
            throw new DuplicateProductSkuException(command.Sku);
        }

        product.UpdateDetails(
            command.Sku,
            command.Name,
            command.Description,
            command.Price);

        await repository.SaveChangesAsync(cancellationToken);
        return ProductDtoMapper.Map(product);
    }
}
