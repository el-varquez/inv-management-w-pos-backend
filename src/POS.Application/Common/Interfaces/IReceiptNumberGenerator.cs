namespace POS.Application.Common.Interfaces;

public interface IReceiptNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}