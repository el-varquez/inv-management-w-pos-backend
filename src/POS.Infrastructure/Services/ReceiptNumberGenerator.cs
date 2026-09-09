using POS.Application.Common.Interfaces;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Services;

public class ReceiptNumberGenerator : IReceiptNumberGenerator
{
    private readonly ISaleRepository _sales;

    public ReceiptNumberGenerator(ISaleRepository saleRepository)
        => _sales = saleRepository;

    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        var prefix = $"R-{DateTime.Now:yyyyMMdd}-";
        var next = await _sales.GetMaxReceiptSequenceAsync(prefix, ct) + 1;
        return $"{prefix}{next:D4}";
    }
}
