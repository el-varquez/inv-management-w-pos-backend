using MediatR;

namespace POS.Application.Utang.Commands.VoidUtangAdjustment;

public record VoidUtangAdjustmentCommand(Guid Id) : IRequest;
