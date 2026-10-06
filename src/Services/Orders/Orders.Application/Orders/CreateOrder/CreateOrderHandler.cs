using Orders.Domain.Entities;
using Orders.Application.Security;

namespace Orders.Application.Orders.CreateOrder;

public sealed class CreateOrderHandler(
    IOrderRepository repository,
    ICurrentUser currentUser)
{
    public async Task<OrderDto> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Items);

        var order = new Order(command.CustomerId, currentUser.UserId);

        foreach (var item in command.Items)
        {
            order.AddItem(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity);
        }

        order.Submit();

        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return OrderDtoMapper.Map(order);
    }
}
