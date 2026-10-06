namespace Orders.Application.Orders.RejectOrder;

public sealed class RejectOrderHandler(IOrderRepository repository)
{
    public async Task HandleAsync(
        RejectOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await repository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(command.OrderId);

        order.Reject();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
