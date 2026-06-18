using MediatR;
using POS.Application.Items.Queries.GetItems;

namespace POS.Application.Items.Queries.GetSellableItems;

/// <summary>
/// Returns the full active catalog (name-ordered, unpaged) for client-side
/// search on the POS register. Distinct from the paged <see cref="GetItemsQuery"/>
/// that backs the admin Items browse-list.
/// </summary>
public record GetSellableItemsQuery : IRequest<IList<ItemDto>>;
