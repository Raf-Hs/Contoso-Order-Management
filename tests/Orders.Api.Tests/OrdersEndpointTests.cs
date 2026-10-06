using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orders.Application.Orders;
using Orders.Domain.Entities;
using Orders.Domain.Enums;
using Xunit;

namespace Orders.Api.Tests;

public sealed class OrdersEndpointTests
{
    [Fact]
    public async Task GetOrders_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new OrdersApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LiveHealth_IsAnonymousAndReturnsTraceId()
    {
        using var factory = new OrdersApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Trace-Id"));
    }

    [Fact]
    public async Task GetOrders_WithoutReadPermission_ReturnsForbidden()
    {
        using var factory = new OrdersApiFactory();
        using var client = CreateClient(factory, "orders.create");

        var response = await client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_UsesCatalogAndCanBeRetrieved()
    {
        using var factory = new OrdersApiFactory();
        using var client = CreateClient(factory, "orders.create,orders.read");

        var createResponse = await client.PostAsJsonAsync("/api/orders", ValidRequest(factory.ProductId));

        await AssertStatusAsync(createResponse, HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);
        Assert.Equal(factory.UserId, created.CreatedBy);
        Assert.Equal("Catalog name", Assert.Single(created.Items).ProductName);
        Assert.Equal(12.50m, created.Total);

        var getResponse = await client.GetAsync($"/api/orders/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithInvalidInput_ReturnsBadRequest()
    {
        using var factory = new OrdersApiFactory();
        using var client = CreateClient(factory, "orders.create");
        var request = new
        {
            customerId = Guid.NewGuid(),
            items = new[] { new { productId = Guid.Empty, quantity = 0 } }
        };

        var response = await client.PostAsJsonAsync("/api/orders", request);

        await AssertStatusAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetOrder_WhenMissing_ReturnsNotFoundProblemDetails()
    {
        using var factory = new OrdersApiFactory();
        using var client = CreateClient(factory, "orders.read");

        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        await AssertStatusAsync(response, HttpStatusCode.NotFound);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ApproveOrder_SecondTransition_ReturnsConflict()
    {
        using var factory = new OrdersApiFactory();
        using var client = CreateClient(factory, "orders.create,orders.approve");
        var createResponse = await client.PostAsJsonAsync("/api/orders", ValidRequest(factory.ProductId));
        var created = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);

        var firstResponse = await client.PostAsync($"/api/orders/{created.Id}/approve", null);
        var secondResponse = await client.PostAsync($"/api/orders/{created.Id}/approve", null);

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WhenCatalogIsUnavailable_ReturnsServiceUnavailable()
    {
        using var factory = new OrdersApiFactory(catalogUnavailable: true);
        using var client = CreateClient(factory, "orders.create");

        var response = await client.PostAsJsonAsync("/api/orders", ValidRequest(factory.ProductId));

        await AssertStatusAsync(response, HttpStatusCode.ServiceUnavailable);
    }

    private static HttpClient CreateClient(OrdersApiFactory factory, string permissions)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", factory.UserId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Permissions", permissions);
        return client;
    }

    private static object ValidRequest(Guid productId) => new
    {
        customerId = Guid.NewGuid(),
        items = new[] { new { productId, quantity = 1 } }
    };

    private static async Task AssertStatusAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == expectedStatus,
            $"Expected {(int)expectedStatus} but got {(int)response.StatusCode}. Response: {body}");
    }

    private sealed class OrdersApiFactory(bool catalogUnavailable = false)
        : WebApplicationFactory<Program>
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid ProductId { get; } = Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IOrderRepository>();
                services.AddSingleton<IOrderRepository>(new TestOrderRepository());
                services.RemoveAll<IProductCatalog>();
                services.AddSingleton<IProductCatalog>(
                    new TestProductCatalog(ProductId, catalogUnavailable));

                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationHandler.SchemeName,
                        _ => { });
            });
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-User", out var userValues)
                || !Guid.TryParse(userValues.FirstOrDefault(), out var userId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim> { new("sub", userId.ToString()) };
            if (Request.Headers.TryGetValue("X-Test-Permissions", out var permissionValues))
            {
                claims.AddRange(permissionValues
                    .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    .Select(permission => new Claim("permission", permission)));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class TestProductCatalog(Guid productId, bool unavailable) : IProductCatalog
    {
        public Task<CatalogProduct?> GetProductAsync(
            Guid requestedProductId,
            CancellationToken cancellationToken = default)
        {
            if (unavailable)
                throw new CatalogUnavailableException();

            CatalogProduct? product = requestedProductId == productId
                ? new CatalogProduct(productId, "Catalog name", 12.50m, 5)
                : null;
            return Task.FromResult(product);
        }
    }

    private sealed class TestOrderRepository : IOrderRepository
    {
        private readonly Dictionary<Guid, Order> _orders = [];

        public Task<Order?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _orders.TryGetValue(id, out var order);
            return Task.FromResult(order);
        }

        public Task<IReadOnlyCollection<Order>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Order>>(_orders.Values.ToArray());
        }

        public Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            _orders.Add(order.Id, order);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
