using Catalog.Domain.Entities;

namespace Catalog.Application.Products.CreateProduct;

public sealed class CreateProductHandler(IProductRepository repository)
{
    public async Task<ProductDto> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (await repository.SkuExistsAsync(command.Sku, cancellationToken: cancellationToken))
            throw new DuplicateProductSkuException(command.Sku);

        var product = new Product(
            command.Sku,
            command.Name,
            command.Description,
            command.Price,
            command.AvailableQuantity);

        await repository.AddAsync(product, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return ProductDtoMapper.Map(product);
    }
}
