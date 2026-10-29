using MediatR;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Features.Products.DTOs;

namespace SalesInventory.Application.Features.Orders.Queries;

public record GetOrdersQuery(
    int Page = 1,
    int PageSize = 10) : IRequest<PagedResult<OrderListItemDto>>;
