using FluentAssertions;
using Moq;
using SalesInventory.Application.Exceptions;
using SalesInventory.Application.Features.Orders.Commands;
using SalesInventory.Application.Interfaces;
using SalesInventory.Domain.Entities;
using SalesInventory.Domain.Enums;

namespace SalesInventory.Tests.Unit.Application.Orders;

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CancelOrderCommandHandler _handler;

    public CancelOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new CancelOrderCommandHandler(
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidOrder_CancelsOrderAndRestoresStockAtomically()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Widget",
            Price = 10.00m,
            StockQuantity = 15,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var order = new SalesOrder
        {
            Id = orderId,
            Status = OrderStatus.Completed,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 50.00m,
            Items = new List<OrderItem>
            {
                new() { Id = Guid.NewGuid(), OrderId = orderId, ProductId = productId, Quantity = 5, UnitPrice = 10.00m }
            }
        };

        _orderRepositoryMock
            .Setup(r => r.GetByIdWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _productRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        await _handler.Handle(new CancelOrderCommand(orderId), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Cancelled);
        product.StockQuantity.Should().Be(20);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _orderRepositoryMock.Verify(r => r.Update(order), Times.Once);
        _productRepositoryMock.Verify(r => r.Update(product), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyCancelledOrder_ThrowsConflictException()
    {
        var orderId = Guid.NewGuid();
        var order = new SalesOrder
        {
            Id = orderId,
            Status = OrderStatus.Cancelled,
            Items = new List<OrderItem>()
        };

        _orderRepositoryMock
            .Setup(r => r.GetByIdWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var act = async () => await _handler.Handle(new CancelOrderCommand(orderId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConcurrencyConflictDuringSave_ThrowsConflictExceptionAndRollsBack()
    {
        var productId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Widget",
            Price = 10.00m,
            StockQuantity = 15,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var order = new SalesOrder
        {
            Id = orderId,
            Status = OrderStatus.Completed,
            Items = new List<OrderItem>
            {
                new() { Id = Guid.NewGuid(), OrderId = orderId, ProductId = productId, Quantity = 5, UnitPrice = 10.00m }
            }
        };

        _orderRepositoryMock
            .Setup(r => r.GetByIdWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _productRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("A concurrency conflict occurred. Please retry the request."));

        var act = async () => await _handler.Handle(new CancelOrderCommand(orderId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        var orderId = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(r => r.GetByIdWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalesOrder?)null);

        var act = async () => await _handler.Handle(new CancelOrderCommand(orderId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
