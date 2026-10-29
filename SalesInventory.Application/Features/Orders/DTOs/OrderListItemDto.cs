using SalesInventory.Domain.Enums;

namespace SalesInventory.Application.Features.Orders.DTOs;

public class OrderListItemDto
{
    public Guid Id { get; init; }
    public DateTime OrderDate { get; init; }
    public decimal TotalAmount { get; init; }
    public OrderStatus Status { get; init; }
    public string StatusName => Status.ToString();
    public int ItemCount { get; init; }
}
