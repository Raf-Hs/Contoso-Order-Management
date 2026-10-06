using System.ComponentModel.DataAnnotations;
using Catalog.Application.Products;
using Catalog.Application.Products.CreateProduct;
using Catalog.Application.Products.DecreaseStock;
using Catalog.Application.Products.GetProduct;
using Catalog.Application.Products.GetProducts;
using Catalog.Application.Products.IncreaseStock;
using Catalog.Application.Products.UpdateProduct;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(
    CreateProductHandler createProduct,
    GetProductHandler getProduct,
    GetProductsHandler getProducts,
    UpdateProductHandler updateProduct,
    IncreaseStockHandler increaseStock,
    DecreaseStockHandler decreaseStock) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<ProductDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var products = await getProducts.HandleAsync(
            new GetProductsQuery(),
            cancellationToken);

        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await getProduct.HandleAsync(
            new GetProductQuery(id),
            cancellationToken);

        return Ok(product);
    }

    [HttpPost]
    [Authorize(Policy = "Catalog.Create")]
    public async Task<ActionResult<ProductDto>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await createProduct.HandleAsync(
            new CreateProductCommand(
                request.Sku,
                request.Name,
                request.Description,
                request.Price,
                request.AvailableQuantity),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Catalog.Update")]
    public async Task<ActionResult<ProductDto>> Update(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await updateProduct.HandleAsync(
            new UpdateProductCommand(
                id,
                request.Sku,
                request.Name,
                request.Description,
                request.Price),
            cancellationToken);

        return Ok(product);
    }

    [HttpPost("{id:guid}/stock/increase")]
    [Authorize(Policy = "Catalog.Stock")]
    public async Task<ActionResult<ProductDto>> IncreaseStock(
        Guid id,
        ChangeStockRequest request,
        CancellationToken cancellationToken)
    {
        var product = await increaseStock.HandleAsync(
            new IncreaseStockCommand(id, request.Quantity),
            cancellationToken);

        return Ok(product);
    }

    [HttpPost("{id:guid}/stock/decrease")]
    [Authorize(Policy = "Catalog.Stock")]
    public async Task<ActionResult<ProductDto>> DecreaseStock(
        Guid id,
        ChangeStockRequest request,
        CancellationToken cancellationToken)
    {
        var product = await decreaseStock.HandleAsync(
            new DecreaseStockCommand(id, request.Quantity),
            cancellationToken);

        return Ok(product);
    }
}

public sealed class CreateProductRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Sku { get; init; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int AvailableQuantity { get; init; }
}

public sealed class UpdateProductRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Sku { get; init; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; init; }
}

public sealed class ChangeStockRequest
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}
