using AutoMapper;
using FluentAssertions;
using Moq;
using SalesInventory.Application.Exceptions;
using SalesInventory.Application.Features.Products.DTOs;
using SalesInventory.Application.Features.Products.Queries;
using SalesInventory.Application.Interfaces;
using SalesInventory.Application.Mapping;
using SalesInventory.Domain.Entities;

namespace SalesInventory.Tests.Unit.Application.Products;

public class GetProductByIdQueryHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly IMapper _mapper;
    private readonly GetProductByIdQueryHandler _handler;

    public GetProductByIdQueryHandlerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ProductProfile>());
        _mapper = config.CreateMapper();
        _handler = new GetProductByIdQueryHandler(_repositoryMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_ProductExists_ReturnsProductDto()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Monitor 4K",
            Price = 399.99m,
            StockQuantity = 15,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _handler.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
        result.Name.Should().Be("Monitor 4K");
        result.Price.Should().Be(399.99m);
        result.StockQuantity.Should().Be(15);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = async () => await _handler.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{productId}*");
    }
}
