using POS.Application.Common.Interfaces;

namespace POS.Infrastructure.Tests.Fakes;

public class FakeInvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private int _next;

    public Task<string> GenerateAsync(CancellationToken ct = default)
        => Task.FromResult($"INV-TEST-{++_next:D4}");
}
