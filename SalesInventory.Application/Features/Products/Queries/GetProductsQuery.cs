using MediatR;
using SalesInventory.Application.Features.Products.DTOs;

namespace SalesInventory.Application.Features.Products.Queries;

public record GetProductsQuery(
    string? SearchTerm,
    int Page = 1,
    int PageSize = 10) : IRequest<PagedResult<ProductListItemDto>>;
