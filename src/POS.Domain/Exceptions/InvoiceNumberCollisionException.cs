namespace POS.Domain.Exceptions;

public class InvoiceNumberCollisionException : Exception
{
    public InvoiceNumberCollisionException()
        : base("Invoice number collided with a concurrent invoice.")
    {
    }
}
