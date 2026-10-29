using MediatR;
using SalesInventory.Application.Features.Products.DTOs;

namespace SalesInventory.Application.Features.Products.Commands;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    decimal Price,
    int StockQuantity) : IRequest<ProductDto>;
