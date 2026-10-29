using AutoMapper;
using MediatR;
using SalesInventory.Application.Features.Products.DTOs;
using SalesInventory.Application.Interfaces;

namespace SalesInventory.Application.Features.Products.Queries;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductListItemDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetProductsQueryHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<ProductListItemDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _productRepository.GetPagedAsync(
            request.SearchTerm,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new PagedResult<ProductListItemDto>
        {
            Items = _mapper.Map<IReadOnlyList<ProductListItemDto>>(items),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
