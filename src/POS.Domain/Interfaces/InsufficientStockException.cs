namespace POS.Domain.Exceptions;

public class InsufficientStockException : DomainException
{
    public InsufficientStockException(string itemName, int requested, int available) : base($"Insufficient stock for '{itemName}'. Requested: {requested}, Available: {available}.") { }
}