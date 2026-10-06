namespace Orders.Application.Orders.ApproveOrder;

public sealed class ApproveOrderHandler(IOrderRepository repository)
{
    public async Task HandleAsync(
        ApproveOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var order = await repository.GetByIdAsync(command.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(command.OrderId);

        order.Approve();
        await repository.SaveChangesAsync(cancellationToken);
    }
}
