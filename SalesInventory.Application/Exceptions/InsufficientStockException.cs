namespace SalesInventory.Application.Exceptions;

public class InsufficientStockException : Exception
{
    public InsufficientStockException(string productName, int requested, int available)
        : base($"Insufficient stock for '{productName}'. Requested: {requested}, Available: {available}.")
    {
    }
}
