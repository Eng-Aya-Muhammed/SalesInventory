using AutoMapper;
using FluentAssertions;
using Moq;
using SalesInventory.Application.Features.Products.Commands;
using SalesInventory.Application.Features.Products.DTOs;
using SalesInventory.Application.Interfaces;
using SalesInventory.Application.Mapping;
using SalesInventory.Domain.Entities;
using Xunit;

namespace SalesInventory.Tests.Unit.Application.Products;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMapper _mapper;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        var config = new MapperConfiguration(cfg => cfg.AddProfile<ProductProfile>());
        _mapper = config.CreateMapper();

        _handler = new CreateProductCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapper);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsProductDto()
    {
        var command = new CreateProductCommand("Widget A", 9.99m, 100);

        _productRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Widget A");
        result.Price.Should().Be(9.99m);
        result.StockQuantity.Should().Be(100);
        result.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsProduct()
    {
        var command = new CreateProductCommand("Widget B", 19.99m, 50);

        await _handler.Handle(command, CancellationToken.None);

        _productRepositoryMock.Verify(
            r => r.AddAsync(It.Is<Product>(p => p.Name == "Widget B"), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsConflictException()
    {
        var command = new CreateProductCommand("Existing Widget", 9.99m, 100);

        _productRepositoryMock
            .Setup(r => r.ExistsByNameAsync("Existing Widget", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<SalesInventory.Application.Exceptions.ConflictException>();
    }
}
