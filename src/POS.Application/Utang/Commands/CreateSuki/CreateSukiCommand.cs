using MediatR;

namespace POS.Application.Utang.Commands.CreateSuki;

public record CreateSukiCommand(string Name, string? Phone) : IRequest<SukiDto>;

public record SukiDto(Guid Id, string Name, string? Phone, decimal Balance);
