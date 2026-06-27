using MediatR;

namespace POS.Application.Platform.Commands.ReactivateTenantUser;

public record ReactivateTenantUserCommand(Guid TenantId, Guid UserId) : IRequest;
