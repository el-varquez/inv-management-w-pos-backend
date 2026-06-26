using MediatR;

namespace POS.Application.Platform.Commands.ReactivateTenant;

public record ReactivateTenantCommand(Guid Id) : IRequest;
