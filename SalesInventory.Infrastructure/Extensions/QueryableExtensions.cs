using Microsoft.EntityFrameworkCore;

namespace SalesInventory.Infrastructure.Extensions;

public static class QueryableExtensions
{
    public static async Task<(IReadOnlyList<T> Items, int TotalCount)> ToPagedAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
