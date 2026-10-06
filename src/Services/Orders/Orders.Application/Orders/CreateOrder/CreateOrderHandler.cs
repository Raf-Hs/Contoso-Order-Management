using Orders.Domain.Entities;
using Orders.Application.Security;

namespace Orders.Application.Orders.CreateOrder;

public sealed class CreateOrderHandler(
    IOrderRepository repository,
    ICurrentUser currentUser,
    IProductCatalog productCatalog)
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
            var product = await productCatalog.GetProductAsync(
                item.ProductId,
                cancellationToken)
                ?? throw new CatalogProductNotFoundException(item.ProductId);

            if (product.AvailableQuantity < item.Quantity)
                throw new InsufficientProductStockException(item.ProductId);

            order.AddItem(
                product.Id,
                product.Name,
                product.Price,
                item.Quantity);
        }

        order.Submit();

        await repository.AddAsync(order, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return OrderDtoMapper.Map(order);
    }
}
