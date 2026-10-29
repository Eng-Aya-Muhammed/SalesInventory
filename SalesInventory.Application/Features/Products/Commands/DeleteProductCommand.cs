using MediatR;

namespace SalesInventory.Application.Features.Products.Commands;

public record DeleteProductCommand(Guid Id) : IRequest;
