using MediatR;

namespace POS.Application.Platform.Commands.EditTenantUser;

public record EditTenantUserCommand(
    Guid TenantId,
    Guid UserId,
    string Name,
    string Email,
    string? Password
) : IRequest;
