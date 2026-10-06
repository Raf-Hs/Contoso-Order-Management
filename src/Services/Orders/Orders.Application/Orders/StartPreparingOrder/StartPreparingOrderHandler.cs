namespace Orders.Application.Orders.StartPreparingOrder;

public sealed class StartPreparingOrderHandler(IOrderRepository repository)
{
    public async Task HandleAsync(
        StartPreparingOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await repository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(command.OrderId);

        order.StartPreparing();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
