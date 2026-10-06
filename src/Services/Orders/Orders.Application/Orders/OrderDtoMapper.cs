using Orders.Domain.Entities;

namespace Orders.Application.Orders;

internal static class OrderDtoMapper
{
    public static OrderDto Map(Order order)
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
