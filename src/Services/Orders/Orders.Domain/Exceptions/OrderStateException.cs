namespace Orders.Domain.Exceptions;

public sealed class OrderStateException(string message) : Exception(message);
