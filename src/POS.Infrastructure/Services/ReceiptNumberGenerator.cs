using POS.Application.Common.Interfaces;
using POS.Domain.Interfaces;

namespace POS.Infrastructure.Services;

public class ReceiptNumberGenerator : IReceiptNumberGenerator
{
    private readonly ITransactionRepository _transactionRepository;

    public ReceiptNumberGenerator(ITransactionRepository transactionRepository)
        => _transactionRepository = transactionRepository;

    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        var prefix = $"R-{DateTime.Now:yyyyMMdd}-";
        var next = await _transactionRepository.GetMaxReceiptSequenceAsync(prefix, ct) + 1;
        return $"{prefix}{next:D4}";
    }
}
