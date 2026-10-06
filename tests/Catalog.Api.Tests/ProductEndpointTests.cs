using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Catalog.Application.Products;
using Catalog.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Catalog.Api.Tests;

public sealed class ProductEndpointTests
{
    [Fact]
    public async Task LiveHealth_IsAnonymousAndReturnsTraceId()
    {
        using var factory = new CatalogApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Trace-Id"));
    }

    [Fact]
    public async Task GetProducts_IsPublic()
    {
        using var factory = new CatalogApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<ProductDto[]>();
        Assert.Single(products!);
    }

    [Fact]
    public async Task CreateProduct_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new CatalogApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products", ValidProduct("SKU-NEW"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithoutPermission_ReturnsForbidden()
    {
        using var factory = new CatalogApiFactory();
        using var client = CreateClient(factory, "catalog.stock");

        var response = await client.PostAsJsonAsync("/api/products", ValidProduct("SKU-NEW"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithPermission_ReturnsCreated()
    {
        using var factory = new CatalogApiFactory();
        using var client = CreateClient(factory, "catalog.create");

        var response = await client.PostAsJsonAsync("/api/products", ValidProduct("SKU-NEW"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal("SKU-NEW", product!.Sku);
    }

    [Fact]
    public async Task CreateProduct_WithDuplicateSku_ReturnsConflict()
    {
        using var factory = new CatalogApiFactory();
        using var client = CreateClient(factory, "catalog.create");

        var response = await client.PostAsJsonAsync(
            "/api/products",
            ValidProduct("SKU-EXISTING"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DecreaseStock_WhenInsufficient_ReturnsConflict()
    {
        using var factory = new CatalogApiFactory();
        using var client = CreateClient(factory, "catalog.stock");

        var response = await client.PostAsJsonAsync(
            $"/api/products/{factory.ExistingProductId}/stock/decrease",
            new { quantity = 99 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static HttpClient CreateClient(CatalogApiFactory factory, string permissions)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add("X-Test-Permissions", permissions);
        return client;
    }

    private static object ValidProduct(string sku) => new
    {
        sku,
        name = "New product",
        description = "Created in an API test",
        price = 12.50m,
        availableQuantity = 5
    };

    private sealed class CatalogApiFactory : WebApplicationFactory<Program>
    {
        public Guid ExistingProductId { get; } = Guid.NewGuid();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IProductRepository>();
                services.AddSingleton<IProductRepository>(
                    new TestProductRepository(ExistingProductId));

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
            if (!Request.Headers.TryGetValue("X-Test-User", out var userValues))
                return Task.FromResult(AuthenticateResult.NoResult());

            var claims = new List<Claim>
            {
                new("sub", userValues.FirstOrDefault() ?? Guid.NewGuid().ToString())
            };
            if (Request.Headers.TryGetValue("X-Test-Permissions", out var permissionValues))
            {
                claims.AddRange(permissionValues
                    .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    .Select(permission => new Claim("permission", permission)));
            }

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, SchemeName)));
        }
    }

    private sealed class TestProductRepository(Guid existingProductId) : IProductRepository
    {
        private readonly Dictionary<Guid, Product> _products = new()
        {
            [existingProductId] = new Product(
                "SKU-EXISTING",
                "Existing product",
                "Seeded for tests",
                10m,
                2)
        };

        public Task<Product?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            _products.TryGetValue(id, out var product);
            return Task.FromResult(product);
        }

        public Task<IReadOnlyCollection<Product>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Product>>(_products.Values.ToArray());
        }

        public Task<bool> SkuExistsAsync(
            string sku,
            Guid? excludingProductId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_products.Values.Any(product =>
                product.Sku == sku && product.Id != excludingProductId));
        }

        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            _products.Add(product.Id, product);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
