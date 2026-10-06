namespace Orders.Application.Orders.GetOrder;

public sealed class GetOrderHandler(IOrderRepository repository)
{
    public async Task<OrderDto> HandleAsync(
        GetOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var order = await repository.GetByIdAsync(query.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(query.OrderId);

        return OrderDtoMapper.Map(order);
    }
}
