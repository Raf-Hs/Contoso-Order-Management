using Microsoft.AspNetCore.Mvc;
using Orders.Application.Orders;
using Orders.Domain.Entities;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderRepository _repository;

    public OrdersController(IOrderRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (order is null)
            return NotFound();

        return Ok(Map(order));
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = new Order(
            request.CustomerId,
            request.CreatedBy);

        foreach (var item in request.Items)
        {
            order.AddItem(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity);
        }

        order.Submit();

        await _repository.AddAsync(
            order,
            cancellationToken);

        await _repository.SaveChangesAsync(
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = order.Id },
            Map(order));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult> Approve(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (order is null)
            return NotFound();

        order.Approve();

        await _repository.SaveChangesAsync(
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult> Reject(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (order is null)
            return NotFound();

        order.Reject();

        await _repository.SaveChangesAsync(
            cancellationToken);

        return NoContent();
    }

    private static OrderDto Map(Order order)
    {
        return new OrderDto(
            order.Id,
            order.CustomerId,
            order.CreatedBy,
            order.Status,
            order.CreatedAt,
            order.Total,
            order.Items
                .Select(item => new OrderItemDto(
                    item.ProductId,
                    item.ProductName,
                    item.UnitPrice,
                    item.Quantity,
                    item.Total))
                .ToList());
    }
}

public sealed record CreateOrderRequest(
    Guid CustomerId,
    Guid CreatedBy,
    IReadOnlyCollection<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity);