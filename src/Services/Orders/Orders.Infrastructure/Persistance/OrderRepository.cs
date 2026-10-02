using Microsoft.EntityFrameworkCore;
using Orders.Application.Orders;
using Orders.Domain.Entities;

namespace Orders.Infrastructure.Persistence;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrdersDbContext _context;

    public OrderRepository(OrdersDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(
            order,
            cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}