namespace Orders.Application.Orders;

public sealed class InsufficientProductStockException(Guid productId)
    : Exception($"Product '{productId}' does not have enough available stock.");
