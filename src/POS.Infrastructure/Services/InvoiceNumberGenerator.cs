using POS.Application.Common.Interfaces;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Services;

public class InvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private readonly IInvoiceRepository _invoices;

    public InvoiceNumberGenerator(IInvoiceRepository invoices) => _invoices = invoices;

    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        var prefix = $"INV-{DateTime.Now:yyyyMMdd}-";
        var next = await _invoices.GetMaxInvoiceSequenceAsync(prefix, ct) + 1;
        return $"{prefix}{next:D4}";
    }
}
