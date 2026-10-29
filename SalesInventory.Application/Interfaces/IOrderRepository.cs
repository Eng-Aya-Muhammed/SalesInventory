using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Domain.Entities;

namespace SalesInventory.Application.Interfaces;

public interface IOrderRepository
{
    Task<SalesOrder?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<OrderListItemDto> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(SalesOrder order, CancellationToken cancellationToken = default);
    void Update(SalesOrder order);
}
