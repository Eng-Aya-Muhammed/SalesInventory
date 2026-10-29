using MediatR;
using SalesInventory.Application.Features.Orders.DTOs;

namespace SalesInventory.Application.Features.Orders.Commands;

public record CreateOrderCommand(IReadOnlyList<CreateOrderItemRequest> Items) : IRequest<OrderDto>;
