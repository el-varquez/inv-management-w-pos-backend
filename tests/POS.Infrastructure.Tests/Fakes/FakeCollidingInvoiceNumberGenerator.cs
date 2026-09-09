using POS.Application.Common.Interfaces;

namespace POS.Infrastructure.Tests.Fakes;

public class FakeCollidingInvoiceNumberGenerator : IInvoiceNumberGenerator
{
    private readonly Queue<string> _numbers;

    public FakeCollidingInvoiceNumberGenerator(params string[] numbers)
        => _numbers = new Queue<string>(numbers);

    public Task<string> GenerateAsync(CancellationToken ct = default)
        => Task.FromResult(_numbers.Dequeue());
}
