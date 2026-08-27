using MediatR;

namespace POS.Application.Utang.Commands.DeleteSuki;

public record DeleteSukiCommand(Guid Id) : IRequest;
