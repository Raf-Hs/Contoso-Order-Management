using Catalog.Domain.Entities;

namespace Catalog.Application.Products;

internal static class ProductDtoMapper
{
    public static ProductDto Map(Product product)
    {
        return new ProductDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.Price,
            product.AvailableQuantity);
    }
}
