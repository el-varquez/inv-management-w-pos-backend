using MediatR;

namespace POS.Application.Platform.Commands.DeactivateTenantUser;

public record DeactivateTenantUserCommand(Guid TenantId, Guid UserId) : IRequest;
