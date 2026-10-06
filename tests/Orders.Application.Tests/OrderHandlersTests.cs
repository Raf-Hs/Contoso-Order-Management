using Orders.Application.Orders;
using Orders.Application.Orders.ApproveOrder;
using Orders.Application.Orders.CancelOrder;
using Orders.Application.Orders.CreateOrder;
using Orders.Application.Orders.GetOrder;
using Orders.Application.Orders.GetOrders;
using Orders.Application.Orders.RejectOrder;
using Orders.Application.Orders.StartPreparingOrder;
using Orders.Application.Security;
using Orders.Domain.Entities;
using Orders.Domain.Enums;
using Xunit;

namespace Orders.Application.Tests;

public sealed class OrderHandlersTests
{
    private readonly FakeOrderRepository _repository = new();
    private readonly FakeProductCatalog _catalog = new();
    private readonly FakeCurrentUser _currentUser = new(Guid.NewGuid());

    [Fact]
    public async Task CreateOrder_UsesCatalogSnapshotAndAuthenticatedUser()
    {
        var productId = Guid.NewGuid();
        _catalog.Add(new CatalogProduct(productId, "Current name", 21.25m, 8));
        var handler = new CreateOrderHandler(_repository, _currentUser, _catalog);

        var result = await handler.HandleAsync(new CreateOrderCommand(
            Guid.NewGuid(),
            [new CreateOrderItem(productId, 2)]));

        Assert.Equal(_currentUser.UserId, result.CreatedBy);
        Assert.Equal(42.50m, result.Total);
        var item = Assert.Single(result.Items);
        Assert.Equal("Current name", item.ProductName);
        Assert.Equal(21.25m, item.UnitPrice);
        Assert.Equal(OrderStatus.PendingApproval, result.Status);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task CreateOrder_WhenProductDoesNotExist_ThrowsNotFoundAndDoesNotSave()
    {
        var handler = new CreateOrderHandler(_repository, _currentUser, _catalog);

        await Assert.ThrowsAsync<CatalogProductNotFoundException>(() =>
            handler.HandleAsync(new CreateOrderCommand(
                Guid.NewGuid(),
                [new CreateOrderItem(Guid.NewGuid(), 1)])));

        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task CreateOrder_WhenStockIsInsufficient_ThrowsConflictAndDoesNotSave()
    {
        var productId = Guid.NewGuid();
        _catalog.Add(new CatalogProduct(productId, "Widget", 10m, 1));
        var handler = new CreateOrderHandler(_repository, _currentUser, _catalog);

        await Assert.ThrowsAsync<InsufficientProductStockException>(() =>
            handler.HandleAsync(new CreateOrderCommand(
                Guid.NewGuid(),
                [new CreateOrderItem(productId, 2)])));

        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task GetOrder_ReturnsOrderWithItems()
    {
        var order = CreateSubmittedOrder();
        _repository.Seed(order);
        var handler = new GetOrderHandler(_repository);

        var result = await handler.HandleAsync(new GetOrderQuery(order.Id));

        Assert.Equal(order.Id, result.Id);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetOrder_WhenMissing_ThrowsOrderNotFound()
    {
        var handler = new GetOrderHandler(_repository);

        await Assert.ThrowsAsync<OrderNotFoundException>(() =>
            handler.HandleAsync(new GetOrderQuery(Guid.NewGuid())));
    }

    [Fact]
    public async Task GetOrders_ReturnsAllOrders()
    {
        _repository.Seed(CreateSubmittedOrder());
        _repository.Seed(CreateSubmittedOrder());
        var handler = new GetOrdersHandler(_repository);

        var results = await handler.HandleAsync(new GetOrdersQuery());

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task ApproveOrder_ChangesStateAndSaves()
    {
        var order = SeedSubmittedOrder();

        await new ApproveOrderHandler(_repository)
            .HandleAsync(new ApproveOrderCommand(order.Id));

        Assert.Equal(OrderStatus.Approved, order.Status);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task RejectOrder_ChangesStateAndSaves()
    {
        var order = SeedSubmittedOrder();

        await new RejectOrderHandler(_repository)
            .HandleAsync(new RejectOrderCommand(order.Id));

        Assert.Equal(OrderStatus.Rejected, order.Status);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task CancelOrder_ChangesStateAndSaves()
    {
        var order = SeedSubmittedOrder();

        await new CancelOrderHandler(_repository)
            .HandleAsync(new CancelOrderCommand(order.Id));

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task StartPreparingOrder_ChangesStateAndSaves()
    {
        var order = SeedSubmittedOrder();
        order.Approve();

        await new StartPreparingOrderHandler(_repository)
            .HandleAsync(new StartPreparingOrderCommand(order.Id));

        Assert.Equal(OrderStatus.Preparing, order.Status);
        Assert.Equal(1, _repository.SaveCount);
    }

    private static Order CreateSubmittedOrder()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid());
        order.AddItem(Guid.NewGuid(), "Widget", 10m, 1);
        order.Submit();
        return order;
    }

    private Order SeedSubmittedOrder()
    {
        var order = CreateSubmittedOrder();
        _repository.Seed(order);
        return order;
    }

    private sealed class FakeCurrentUser(Guid userId) : ICurrentUser
    {
        public Guid UserId { get; } = userId;
    }

    private sealed class FakeProductCatalog : IProductCatalog
    {
        private readonly Dictionary<Guid, CatalogProduct> _products = [];

        public void Add(CatalogProduct product) => _products.Add(product.Id, product);

        public Task<CatalogProduct?> GetProductAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
        {
            _products.TryGetValue(productId, out var product);
            return Task.FromResult(product);
        }
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly Dictionary<Guid, Order> _orders = [];

        public int SaveCount { get; private set; }

        public void Seed(Order order) => _orders.Add(order.Id, order);

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

        public Task AddAsync(
            Order order,
            CancellationToken cancellationToken = default)
        {
            _orders.Add(order.Id, order);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
