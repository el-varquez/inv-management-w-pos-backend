namespace POS.Domain.Exceptions;

public class ReceiptNumberCollisionException : Exception
{
    public ReceiptNumberCollisionException()
        : base("Receipt number collided with a concurrent sale.")
    {
    }
}
