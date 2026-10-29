using FluentAssertions;
using Moq;
using SalesInventory.Application.Features.Orders.DTOs;
using SalesInventory.Application.Features.Orders.Queries;
using SalesInventory.Application.Interfaces;
using SalesInventory.Domain.Enums;

namespace SalesInventory.Tests.Unit.Application.Orders;

public class GetOrdersQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly GetOrdersQueryHandler _handler;

    public GetOrdersQueryHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _handler = new GetOrdersQueryHandler(_orderRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedOrders_WithMappedProperties()
    {
        var dtos = new List<OrderListItemDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OrderDate = DateTime.UtcNow,
                TotalAmount = 100m,
                Status = OrderStatus.Completed,
                ItemCount = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                OrderDate = DateTime.UtcNow,
                TotalAmount = 25m,
                Status = OrderStatus.Cancelled,
                ItemCount = 1
            }
        };

        _orderRepositoryMock
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<OrderListItemDto>)dtos, 2));

        var result = await _handler.Handle(new GetOrdersQuery(1, 10), CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
        result.Items[0].TotalAmount.Should().Be(100m);
        result.Items[0].ItemCount.Should().Be(1);
        result.Items[1].Status.Should().Be(OrderStatus.Cancelled);
        result.Items[1].ItemCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyPagedResult()
    {
        _orderRepositoryMock
            .Setup(r => r.GetPagedAsync(1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<OrderListItemDto>)new List<OrderListItemDto>(), 0));

        var result = await _handler.Handle(new GetOrdersQuery(1, 10), CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }
}
