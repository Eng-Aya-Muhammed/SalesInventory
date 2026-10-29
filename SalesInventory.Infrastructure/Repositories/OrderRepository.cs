using Microsoft.EntityFrameworkCore;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Interfaces;
using SalesInventory.Domain.Entities;
using SalesInventory.Infrastructure.Extensions;
using SalesInventory.Infrastructure.Persistence;

namespace SalesInventory.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SalesOrder?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<OrderListItemDto> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new OrderListItemDto
            {
                Id          = o.Id,
                OrderDate   = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status      = o.Status,
                ItemCount   = o.Items.Count
            })
            .ToPagedAsync(page, pageSize, cancellationToken);
    }

    public async Task AddAsync(SalesOrder order, CancellationToken cancellationToken = default)
        => await _context.Orders.AddAsync(order, cancellationToken);

    public void Update(SalesOrder order)
        => _context.Orders.Update(order);
}
