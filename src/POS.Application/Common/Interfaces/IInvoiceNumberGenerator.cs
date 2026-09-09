namespace POS.Application.Common.Interfaces;

public interface IInvoiceNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}
