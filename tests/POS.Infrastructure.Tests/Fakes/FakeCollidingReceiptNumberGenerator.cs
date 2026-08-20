using POS.Application.Common.Interfaces;

namespace POS.Infrastructure.Tests.Fakes;

public class FakeCollidingReceiptNumberGenerator : IReceiptNumberGenerator
{
    private readonly Queue<string> _numbers;

    public FakeCollidingReceiptNumberGenerator(params string[] numbers)
        => _numbers = new Queue<string>(numbers);

    public Task<string> GenerateAsync(CancellationToken ct = default)
        => Task.FromResult(_numbers.Dequeue());
}
