namespace Catalog.Application.Products;

public sealed class DuplicateProductSkuException(
    string sku,
    Exception? innerException = null)
    : Exception($"A product with SKU '{sku}' already exists.", innerException);
