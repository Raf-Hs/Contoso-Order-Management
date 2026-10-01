using Orders.Domain.Enums;

namespace Orders.Application.Orders;

public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    Guid CreatedBy,
    OrderStatus Status,
    DateTime CreatedAt,
    decimal Total,
    IReadOnlyCollection<OrderItemDto> Items);

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Total);