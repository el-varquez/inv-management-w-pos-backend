using MediatR;

namespace POS.Domain.Common;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}