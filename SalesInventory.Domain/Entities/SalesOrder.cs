using SalesInventory.Domain.Enums;

namespace SalesInventory.Domain.Entities;

public class SalesOrder
{
    public Guid Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
