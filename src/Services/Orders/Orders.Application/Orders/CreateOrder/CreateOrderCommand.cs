namespace Orders.Application.Orders.CreateOrder;

public sealed record CreateOrderCommand(
    Guid CustomerId,
    IReadOnlyCollection<CreateOrderItem> Items);

public sealed record CreateOrderItem(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity);
