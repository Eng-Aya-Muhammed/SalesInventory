using AutoMapper;
using FluentAssertions;
using Moq;
using SalesInventory.Application.Exceptions;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Features.Orders.Queries;
using SalesInventory.Application.Interfaces;
using SalesInventory.Application.Mapping;
using SalesInventory.Domain.Entities;
using SalesInventory.Domain.Enums;

namespace SalesInventory.Tests.Unit.Application.Orders;

public class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly IMapper _mapper;
    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        var config = new MapperConfiguration(cfg => cfg.AddProfile<OrderProfile>());
        _mapper = config.CreateMapper();
        _handler = new GetOrderByIdQueryHandler(_orderRepositoryMock.Object, _mapper);
    }

    [Fact]
    public async Task Handle_OrderExists_ReturnsOrderDtoWithItems()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var order = new SalesOrder
        {
            Id = orderId,
            OrderDate = DateTime.UtcNow,
            TotalAmount = 59.98m,
            Status = OrderStatus.Completed,
            Items = new List<OrderItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    ProductId = productId,
                    Quantity = 2,
                    UnitPrice = 29.99m,
                    Product = new Product
                    {
                        Id = productId,
                        Name = "Wireless Mouse",
                        Price = 29.99m,
                        StockQuantity = 100
                    }
                }
            }
        };

        _orderRepositoryMock
            .Setup(r => r.GetByIdWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(new GetOrderByIdQuery(orderId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(orderId);
        result.TotalAmount.Should().Be(59.98m);
        result.Status.Should().Be(OrderStatus.Completed);
        result.Items.Should().HaveCount(1);
        result.Items[0].ProductName.Should().Be("Wireless Mouse");
        result.Items[0].Quantity.Should().Be(2);
        result.Items[0].UnitPrice.Should().Be(29.99m);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        var orderId = Guid.NewGuid();
        _orderRepositoryMock
            .Setup(r => r.GetByIdWithItemsAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalesOrder?)null);

        var act = async () => await _handler.Handle(new GetOrderByIdQuery(orderId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{orderId}*");
    }
}
