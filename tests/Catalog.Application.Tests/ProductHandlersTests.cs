using Catalog.Application.Products;
using Catalog.Application.Products.CreateProduct;
using Catalog.Application.Products.DecreaseStock;
using Catalog.Application.Products.GetProduct;
using Catalog.Application.Products.GetProducts;
using Catalog.Application.Products.IncreaseStock;
using Catalog.Application.Products.UpdateProduct;
using Catalog.Domain.Entities;
using Xunit;

namespace Catalog.Application.Tests;

public sealed class ProductHandlersTests
{
    private readonly FakeProductRepository _repository = new();

    [Fact]
    public async Task CreateProduct_AddsAndReturnsProduct()
    {
        var handler = new CreateProductHandler(_repository);

        var result = await handler.HandleAsync(
            new CreateProductCommand("SKU-1", "Widget", "Description", 5m, 3));

        Assert.Equal("SKU-1", result.Sku);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task CreateProduct_WhenSkuAlreadyExists_ThrowsConflict()
    {
        _repository.Seed(new Product("SKU-1", "Widget", "Description", 5m, 3));
        var handler = new CreateProductHandler(_repository);

        await Assert.ThrowsAsync<DuplicateProductSkuException>(() =>
            handler.HandleAsync(
                new CreateProductCommand("SKU-1", "Other", "Description", 5m, 3)));

        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task GetProduct_ReturnsExistingProduct()
    {
        var product = new Product("SKU-1", "Widget", "Description", 5m, 3);
        _repository.Seed(product);

        var result = await new GetProductHandler(_repository)
            .HandleAsync(new GetProductQuery(product.Id));

        Assert.Equal(product.Id, result.Id);
    }

    [Fact]
    public async Task GetProduct_WhenMissing_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            new GetProductHandler(_repository)
                .HandleAsync(new GetProductQuery(Guid.NewGuid())));
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProducts()
    {
        _repository.Seed(new Product("SKU-1", "One", "Description", 1m, 1));
        _repository.Seed(new Product("SKU-2", "Two", "Description", 2m, 2));

        var results = await new GetProductsHandler(_repository)
            .HandleAsync(new GetProductsQuery());

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task UpdateProduct_UpdatesDetailsAndSaves()
    {
        var product = new Product("SKU-1", "Widget", "Old", 1m, 3);
        _repository.Seed(product);

        var result = await new UpdateProductHandler(_repository)
            .HandleAsync(new UpdateProductCommand(
                product.Id,
                "SKU-2",
                "Updated",
                "New description",
                9m));

        Assert.Equal("SKU-2", result.Sku);
        Assert.Equal("Updated", result.Name);
        Assert.Equal(3, result.AvailableQuantity);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task UpdateProduct_WhenSkuBelongsToAnotherProduct_ThrowsConflict()
    {
        var first = new Product("SKU-1", "One", "Description", 1m, 1);
        var second = new Product("SKU-2", "Two", "Description", 2m, 2);
        _repository.Seed(first);
        _repository.Seed(second);

        await Assert.ThrowsAsync<DuplicateProductSkuException>(() =>
            new UpdateProductHandler(_repository).HandleAsync(
                new UpdateProductCommand(second.Id, "SKU-1", "Two", "Description", 2m)));
    }

    [Fact]
    public async Task IncreaseAndDecreaseStock_UseDomainRulesAndPersist()
    {
        var product = new Product("SKU-1", "Widget", "Description", 1m, 3);
        _repository.Seed(product);

        await new IncreaseStockHandler(_repository)
            .HandleAsync(new IncreaseStockCommand(product.Id, 2));
        await new DecreaseStockHandler(_repository)
            .HandleAsync(new DecreaseStockCommand(product.Id, 4));

        Assert.Equal(1, product.AvailableQuantity);
        Assert.Equal(2, _repository.SaveCount);
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly Dictionary<Guid, Product> _products = [];

        public int SaveCount { get; private set; }

        public void Seed(Product product) => _products.Add(product.Id, product);

        public Task<Product?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _products.TryGetValue(id, out var product);
            return Task.FromResult(product);
        }

        public Task<IReadOnlyCollection<Product>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Product>>(_products.Values.ToArray());
        }

        public Task<bool> SkuExistsAsync(
            string sku,
            Guid? excludingProductId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_products.Values.Any(product =>
                product.Sku == sku && product.Id != excludingProductId));
        }

        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            _products.Add(product.Id, product);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
