using FluentAssertions;
using Moq;
using SalesInventory.Application.Exceptions;
using SalesInventory.Application.Features.Products.Commands;
using SalesInventory.Application.Interfaces;
using SalesInventory.Domain.Entities;

namespace SalesInventory.Tests.Unit.Application.Products;

public class DeleteProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeleteProductCommandHandler _handler;

    public DeleteProductCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteProductCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidProductWithoutOrders_DeletesProduct()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Standalone Product",
            Price = 10m,
            StockQuantity = 5
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _productRepositoryMock
            .Setup(r => r.HasOrderReferencesAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _handler.Handle(new DeleteProductCommand(productId), CancellationToken.None);

        _productRepositoryMock.Verify(r => r.Delete(product), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductWithHistoricalOrders_ThrowsConflictException()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Ordered Product",
            Price = 10m,
            StockQuantity = 5
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _productRepositoryMock
            .Setup(r => r.HasOrderReferencesAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _handler.Handle(new DeleteProductCommand(productId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _productRepositoryMock.Verify(r => r.Delete(It.IsAny<Product>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var missingId = Guid.NewGuid();

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var act = async () => await _handler.Handle(new DeleteProductCommand(missingId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
