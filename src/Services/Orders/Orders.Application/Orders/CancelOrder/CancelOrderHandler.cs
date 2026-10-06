namespace Orders.Application.Orders.CancelOrder;

public sealed class CancelOrderHandler(IOrderRepository repository)
{
    public async Task HandleAsync(
        CancelOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await repository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(command.OrderId);

        order.Cancel();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
