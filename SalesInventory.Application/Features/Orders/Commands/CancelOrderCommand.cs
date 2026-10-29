using MediatR;

namespace SalesInventory.Application.Features.Orders.Commands;

public record CancelOrderCommand(Guid OrderId) : IRequest;
