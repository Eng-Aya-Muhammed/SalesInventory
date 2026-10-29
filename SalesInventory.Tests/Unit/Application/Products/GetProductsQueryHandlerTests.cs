using AutoMapper;
using FluentAssertions;
using Moq;
using SalesInventory.Application.Features.Products.DTOs;
using SalesInventory.Application.Features.Products.Queries;
using SalesInventory.Application.Interfaces;
using SalesInventory.Application.Mapping;
using SalesInventory.Domain.Entities;

namespace SalesInventory.Tests.Unit.Application.Products;

public class GetProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly IMapper _mapper;
    private readonly GetProductsQueryHandler _handler;

    public GetProductsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ProductProfile>());
        _mapper = config.CreateMapper();
        _handler = new GetProductsQueryHandler(_repositoryMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult_WithCorrectMetadata()
    {
        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Alpha", Price = 10m, StockQuantity = 5, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Beta",  Price = 20m, StockQuantity = 10, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        _repositoryMock
            .Setup(r => r.GetPagedAsync(null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Product>)products, 2));

        var result = await _handler.Handle(new GetProductsQuery(null, 1, 10), CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_PassesSearchTermToRepository()
    {
        _repositoryMock
            .Setup(r => r.GetPagedAsync("keyboard", 1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Product>)new List<Product>(), 0));

        await _handler.Handle(new GetProductsQuery("keyboard", 1, 5), CancellationToken.None);

        _repositoryMock.Verify(
            r => r.GetPagedAsync("keyboard", 1, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyPagedResult()
    {
        _repositoryMock
            .Setup(r => r.GetPagedAsync(It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Product>)new List<Product>(), 0));

        var result = await _handler.Handle(new GetProductsQuery(null, 1, 10), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }
}
