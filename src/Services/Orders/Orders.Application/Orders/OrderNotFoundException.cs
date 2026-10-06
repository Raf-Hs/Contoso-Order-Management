namespace Orders.Application.Orders;

public sealed class OrderNotFoundException(Guid orderId)
    : Exception($"Order '{orderId}' was not found.");
