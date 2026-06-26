using MediatR;

namespace POS.Application.Platform.Commands.CreateTenant;

public record CreateTenantCommand(
    string BusinessName,
    string AdminName,
    string AdminEmail,
    string AdminPassword,
    int? CashierCap
) : IRequest<Guid>;
