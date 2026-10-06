using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Orders.Application.Orders;
using Orders.Application.Orders.ApproveOrder;
using Orders.Application.Orders.CancelOrder;
using Orders.Application.Orders.CreateOrder;
using Orders.Application.Orders.GetOrder;
using Orders.Application.Orders.GetOrders;
using Orders.Application.Orders.RejectOrder;
using Orders.Application.Orders.StartPreparingOrder;

namespace Orders.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly CreateOrderHandler _createOrder;
    private readonly GetOrderHandler _getOrder;
    private readonly GetOrdersHandler _getOrders;
    private readonly ApproveOrderHandler _approveOrder;
    private readonly RejectOrderHandler _rejectOrder;
    private readonly CancelOrderHandler _cancelOrder;
    private readonly StartPreparingOrderHandler _startPreparingOrder;

    public OrdersController(
        CreateOrderHandler createOrder,
        GetOrderHandler getOrder,
        GetOrdersHandler getOrders,
        ApproveOrderHandler approveOrder,
        RejectOrderHandler rejectOrder,
        CancelOrderHandler cancelOrder,
        StartPreparingOrderHandler startPreparingOrder)
    {
        _createOrder = createOrder;
        _getOrder = getOrder;
        _getOrders = getOrders;
        _approveOrder = approveOrder;
        _rejectOrder = rejectOrder;
        _cancelOrder = cancelOrder;
        _startPreparingOrder = startPreparingOrder;
    }

    [HttpGet]
    [Authorize(Policy = "Orders.Read")]
    public async Task<ActionResult<IReadOnlyCollection<OrderDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var orders = await _getOrders.HandleAsync(
            new GetOrdersQuery(),
            cancellationToken);

        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Orders.Read")]
    public async Task<ActionResult<OrderDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await _getOrder.HandleAsync(
            new GetOrderQuery(id),
            cancellationToken);

        return Ok(order);
    }

    [HttpPost]
    [Authorize(Policy = "Orders.Create")]
    public async Task<ActionResult<OrderDto>> Create(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _createOrder.HandleAsync(new CreateOrderCommand(
            request.CustomerId,
            request.Items.Select(item => new CreateOrderItem(
                item.ProductId,
                item.Quantity)).ToArray()),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = order.Id },
            order);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "Orders.Approve")]
    public async Task<ActionResult> Approve(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _approveOrder.HandleAsync(
            new ApproveOrderCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "Orders.Reject")]
    public async Task<ActionResult> Reject(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _rejectOrder.HandleAsync(
            new RejectOrderCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "Orders.Cancel")]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _cancelOrder.HandleAsync(
            new CancelOrderCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/start-preparing")]
    [Authorize(Policy = "Orders.StartPreparing")]
    public async Task<IActionResult> StartPreparing(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _startPreparingOrder.HandleAsync(
            new StartPreparingOrderCommand(id),
            cancellationToken);

        return NoContent();
    }
}

public sealed record CreateOrderRequest(
    Guid CustomerId,
    [property: Required] IReadOnlyCollection<CreateOrderItemRequest> Items)
    : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CustomerId == Guid.Empty)
            yield return new ValidationResult(
                "CustomerId is required.",
                [nameof(CustomerId)]);

        if (Items is null || Items.Count == 0)
            yield return new ValidationResult(
                "At least one order item is required.",
                [nameof(Items)]);
    }
}

public sealed record CreateOrderItemRequest(
    Guid ProductId,
    [property: Range(1, int.MaxValue)] int Quantity)
    : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProductId == Guid.Empty)
            yield return new ValidationResult(
                "ProductId is required.",
                [nameof(ProductId)]);
    }
}
