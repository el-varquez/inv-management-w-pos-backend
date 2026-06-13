using MediatR;

namespace POS.Application.Items.Commands.DeleteItem;

public record DeleteItemCommand(Guid Id) : IRequest;