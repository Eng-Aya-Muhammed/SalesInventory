using MediatR;
using SalesInventory.Application.Features.Products.DTOs;

namespace SalesInventory.Application.Features.Products.Queries;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;
