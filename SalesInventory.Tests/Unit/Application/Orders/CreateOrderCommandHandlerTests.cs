using AutoMapper;
using FluentAssertions;
using Moq;
using SalesInventory.Application.Exceptions;
using SalesInventory.Application.Features.Orders.Commands;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Interfaces;
using SalesInventory.Application.Mapping;
using SalesInventory.Domain.Entities;
using SalesInventory.Domain.Enums;

namespace SalesInventory.Tests.Unit.Application.Orders;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMapper _mapper;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProductProfile>();
            cfg.AddProfile<OrderProfile>();
        });
        _mapper = config.CreateMapper();

        _handler = new CreateOrderCommandHandler(
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapper);
    }

    [Fact]
    public async Task Handle_ValidOrder_DeductsStockAndReturnsOrderDto()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Widget",
            Price = 10.00m,
            StockQuantity = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var command = new CreateOrderCommand(
            new List<CreateOrderItemRequest> { new() { ProductId = productId, Quantity = 5 } });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(50.00m);
        result.Status.Should().Be(OrderStatus.Completed);
        product.StockQuantity.Should().Be(15);

        _productRepositoryMock.Verify(r => r.Update(product), Times.Once);
        _orderRepositoryMock.Verify(r => r.AddAsync(It.IsAny<SalesOrder>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateProductIds_AggregatesQuantitiesAndDeductsOnce()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Widget",
            Price = 15.00m,
            StockQuantity = 20,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var command = new CreateOrderCommand(
            new List<CreateOrderItemRequest>
            {
                new() { ProductId = productId, Quantity = 2 },
                new() { ProductId = productId, Quantity = 3 }
            });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalAmount.Should().Be(75.00m);
        result.Items.Should().HaveCount(1);
        result.Items.First().Quantity.Should().Be(5);
        product.StockQuantity.Should().Be(15);
    }

    [Fact]
    public async Task Handle_InsufficientStock_ThrowsInsufficientStockExceptionAndRollsBack()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Widget",
            Price = 10.00m,
            StockQuantity = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var command = new CreateOrderCommand(
            new List<CreateOrderItemRequest> { new() { ProductId = productId, Quantity = 10 } });

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientStockException>();
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ConcurrencyConflictDuringSave_RollsBackTransaction()
    {
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Widget",
            Price = 10.00m,
            StockQuantity = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("A concurrency conflict occurred. Please retry the request."));

        var command = new CreateOrderCommand(
            new List<CreateOrderItemRequest> { new() { ProductId = productId, Quantity = 5 } });

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _unitOfWorkMock.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var missingProductId = Guid.NewGuid();

        _productRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var command = new CreateOrderCommand(
            new List<CreateOrderItemRequest> { new() { ProductId = missingProductId, Quantity = 1 } });

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
