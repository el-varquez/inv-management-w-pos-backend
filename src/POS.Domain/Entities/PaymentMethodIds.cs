namespace POS.Domain.Entities;

public static class PaymentMethodIds
{
    public static readonly Guid Cash = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid EWallet = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid Utang = Guid.Parse("00000000-0000-0000-0000-000000000003");
}
