using MediatR;

namespace POS.Application.Platform.Commands.SuspendTenant;

public record SuspendTenantCommand(Guid Id) : IRequest;
