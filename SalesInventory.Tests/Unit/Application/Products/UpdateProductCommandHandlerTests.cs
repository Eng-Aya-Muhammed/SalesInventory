using AutoMapper;
using FluentAssertions;
using Moq;
using SalesInventory.Application.Exceptions;
using SalesInventory.Application.Features.Products.Commands;
using SalesInventory.Application.Features.Products.DTOs;
using SalesInventory.Application.Interfaces;
using SalesInventory.Application.Mapping;
using SalesInventory.Domain.Entities;

namespace SalesInventory.Tests.Unit.Application.Products;

public class UpdateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMapper _mapper;
    private readonly UpdateProductCommandHandler _handler;

    public UpdateProductCommandHandlerTests()
    {
        _repositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ProductProfile>());
        _mapper = config.CreateMapper();
        _handler = new UpdateProductCommandHandler(_repositoryMock.Object, _unitOfWorkMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_ExistingProduct_UpdatesFieldsAndReturnsDto()
    {
        var productId = Guid.NewGuid();
        var existing = new Product
        {
            Id = productId,
            Name = "Old Name",
            Price = 10.00m,
            StockQuantity = 5,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _repositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var command = new UpdateProductCommand(productId, "New Name", 25.00m, 20);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Name");
        result.Price.Should().Be(25.00m);
        result.StockQuantity.Should().Be(20);
        existing.Name.Should().Be("New Name");
        existing.Price.Should().Be(25.00m);
        existing.StockQuantity.Should().Be(20);
        existing.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _repositoryMock.Verify(r => r.Update(existing), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new UpdateProductCommand(missingId, "X", 1.00m, 1);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_DoesNotCallRepository_AddOrDelete()
    {
        var productId = Guid.NewGuid();
        var existing = new Product
        {
            Id = productId, Name = "Widget", Price = 5.00m,
            StockQuantity = 10, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _repositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _handler.Handle(new UpdateProductCommand(productId, "Widget", 5.00m, 10), CancellationToken.None);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.Delete(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateNameOnAnotherProduct_ThrowsConflictException()
    {
        var productId = Guid.NewGuid();
        var existing = new Product
        {
            Id = productId, Name = "Widget", Price = 5.00m,
            StockQuantity = 10, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _repositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repositoryMock
            .Setup(r => r.ExistsByNameAsync("Taken Name", productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _handler.Handle(new UpdateProductCommand(productId, "Taken Name", 5.00m, 10), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _repositoryMock.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
