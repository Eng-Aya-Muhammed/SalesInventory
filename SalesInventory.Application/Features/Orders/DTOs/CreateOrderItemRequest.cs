namespace SalesInventory.Application.Features.Orders.DTOs;

public class CreateOrderItemRequest
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}
