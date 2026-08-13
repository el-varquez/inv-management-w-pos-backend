using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Items.Commands.CreateItem;
using POS.Application.Items.Commands.DeleteItem;
using POS.Application.Items.Commands.UpdateItem;
using POS.Application.Items.Queries.GetItems;
using POS.Application.Items.Queries.GetSellableItems;
using POS.Application.Items.Queries.SearchItems;

namespace POS.API.Controller;

[ApiController]
[Route("api/items")]
[Authorize]
public class ItemsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ItemsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] bool? isComposite)
        => Ok(await _mediator.Send(new GetItemsQuery(page, pageSize, isComposite)));

    [HttpGet("sellable")]
    public async Task<IActionResult> GetSellable()
        => Ok(await _mediator.Send(new GetSellableItemsQuery()));

    [HttpGet("search")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Search(
        [FromQuery] string? term,
        [FromQuery] int? limit)
        => Ok(await _mediator.Send(new SearchItemsQuery(term ?? string.Empty, limit)));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateItemCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateItemCommand command)
    {
        await _mediator.Send(command with { Id = id });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteItemCommand(id));
        return NoContent();
    }
}