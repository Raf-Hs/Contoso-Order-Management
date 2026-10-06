using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Orders.Application.Orders;

namespace Orders.Infrastructure.Integrations;

public sealed class CatalogProductClient(HttpClient httpClient) : IProductCatalog
{
    public async Task<CatalogProduct?> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                $"api/products/{productId}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
                throw new CatalogUnavailableException();

            var product = await response.Content.ReadFromJsonAsync<CatalogProductResponse>(
                cancellationToken: cancellationToken);

            if (product is null)
                throw new CatalogUnavailableException();

            return new CatalogProduct(
                product.Id,
                product.Name,
                product.Price,
                product.AvailableQuantity);
        }
        catch (HttpRequestException exception)
        {
            throw new CatalogUnavailableException(exception);
        }
        catch (JsonException exception)
        {
            throw new CatalogUnavailableException(exception);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new CatalogUnavailableException(exception);
        }
    }

    private sealed record CatalogProductResponse(
        Guid Id,
        string Sku,
        string Name,
        string Description,
        decimal Price,
        int AvailableQuantity);
}
