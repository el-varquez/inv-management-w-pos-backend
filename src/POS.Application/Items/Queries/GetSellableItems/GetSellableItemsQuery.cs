using MediatR;
using POS.Application.Items.Queries.GetItems;

namespace POS.Application.Items.Queries.GetSellableItems;

public record GetSellableItemsQuery : IRequest<IList<ItemDto>>;
