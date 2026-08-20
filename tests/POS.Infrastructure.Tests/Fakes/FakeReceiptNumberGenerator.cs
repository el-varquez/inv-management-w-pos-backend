using POS.Application.Common.Interfaces;

namespace POS.Infrastructure.Tests.Fakes;

public class FakeReceiptNumberGenerator : IReceiptNumberGenerator
{
    private int _next;

    public Task<string> GenerateAsync(CancellationToken ct = default)
        => Task.FromResult($"R-TEST-{++_next:D4}");
}
