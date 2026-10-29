using MediatR;
using SalesInventory.Application.Features.Products.DTOs;

namespace SalesInventory.Application.Features.Products.Commands;

public record CreateProductCommand(
    string Name,
    decimal Price,
    int StockQuantity) : IRequest<ProductDto>;
