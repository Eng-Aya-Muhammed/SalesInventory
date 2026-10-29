using MediatR;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Features.Products.DTOs;
using SalesInventory.Application.Interfaces;

namespace SalesInventory.Application.Features.Orders.Queries;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResult<OrderListItemDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<PagedResult<OrderListItemDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _orderRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            cancellationToken);

        return new PagedResult<OrderListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
