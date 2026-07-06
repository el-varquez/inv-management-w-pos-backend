using POS.Application.Common.Interfaces;

namespace POS.Infrastructure.Tests.Fakes;

public class FakeReceiptNumberGenerator : IReceiptNumberGenerator
{
    public Task<string> GenerateAsync(CancellationToken ct = default)
        => Task.FromResult("R-TEST-0001");
}
