namespace Orders.Application.Orders;

public sealed class CatalogUnavailableException(Exception? innerException = null)
    : Exception("Catalog is currently unavailable.", innerException);
