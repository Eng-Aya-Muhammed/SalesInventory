using MediatR;
using SalesInventory.Application.Features.Orders.DTOs;

namespace SalesInventory.Application.Features.Orders.Queries;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderDto>;
