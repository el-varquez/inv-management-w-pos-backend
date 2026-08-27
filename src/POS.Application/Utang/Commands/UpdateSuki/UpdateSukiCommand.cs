using MediatR;

namespace POS.Application.Utang.Commands.UpdateSuki;

public record UpdateSukiCommand(Guid Id, string Name, string? Phone) : IRequest;
