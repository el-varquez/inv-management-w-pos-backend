using MediatR;

namespace POS.Application.Utang.Commands.CreateUtangAdjustment;

public record CreateUtangAdjustmentCommand(Guid SukiId, decimal Amount, string Note)
    : IRequest<Guid>;
