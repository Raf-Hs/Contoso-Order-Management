namespace Orders.Application.Orders.GetOrders;

public sealed class GetOrdersHandler(IOrderRepository repository)
{
    public async Task<IReadOnlyCollection<OrderDto>> HandleAsync(
        GetOrdersQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var orders = await repository.GetAllAsync(cancellationToken);
        return orders.Select(OrderDtoMapper.Map).ToArray();
    }
}
