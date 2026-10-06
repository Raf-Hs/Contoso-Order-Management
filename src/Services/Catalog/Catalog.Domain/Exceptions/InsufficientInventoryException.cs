namespace Catalog.Domain.Exceptions;

public sealed class InsufficientInventoryException(string message) : Exception(message);
