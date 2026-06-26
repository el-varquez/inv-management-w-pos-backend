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
        var todayCount = await _transactionRepository.GetCountForTodayAsync(ct);
        var sequence = (todayCount + 1).ToString("D4");
        return $"R-{DateTime.UtcNow:yyyyMMdd}-{sequence}";
    }
}