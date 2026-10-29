using AutoMapper;
using SalesInventory.Application.Features.Products.DTOs;
using SalesInventory.Domain.Entities;

namespace SalesInventory.Application.Mapping;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductDto>();
        CreateMap<Product, ProductListItemDto>();
    }
}
