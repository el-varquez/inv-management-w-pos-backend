using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Inventory.Commands.AddStock;
using POS.Application.Inventory.Commands.AdjustStock;
using POS.Application.Inventory.Commands.CompleteInventoryCount;
using POS.Application.Inventory.Commands.CreateInventoryCount;
using POS.Application.Inventory.Commands.SetCompositeItem;
using POS.Application.Inventory.Queries.GetInventoryHistory;
using POS.Application.Inventory.Queries.GetItemComponents;
using POS.Application.Inventory.Queries.GetInventoryValuation;
using POS.Application.Inventory.Queries.GetLowStockItems;
using POS.Application.Inventory.Queries.GetStockLevels;
using POS.Domain.Enums;

namespace POS.API.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = "Admin")]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator) => _mediator = mediator;

    [HttpGet("stock-levels")]
    public async Task<IActionResult> GetStockLevels(
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
        => Ok(await _mediator.Send(new GetStockLevelsQuery(page, pageSize)));

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock()
        => Ok(await _mediator.Send(new GetLowStockItemsQuery()));

    [HttpPost("add-stock")]
    public async Task<IActionResult> AddStock([FromBody] AddStockCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { id });
    }

    [HttpPost("adjust-stock")]
    public async Task<IActionResult> AdjustStock([FromBody] AdjustStockCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { id });
    }

    [HttpPost("count")]
    public async Task<IActionResult> CreateCount([FromBody] CreateInventoryCountCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { id });
    }

    [HttpPost("count/{id:guid}/complete")]
    public async Task<IActionResult> CompleteCount(
        Guid id,
        [FromBody] IList<CountLineInput> lines)
    {
        await _mediator.Send(new CompleteInventoryCountCommand(id, lines));
        return NoContent();
    }

    [HttpGet("items/{id:guid}/components")]
    public async Task<IActionResult> GetComponents(Guid id)
        => Ok(await _mediator.Send(new GetItemComponentsQuery(id)));

    [HttpPost("items/{id:guid}/components")]
    public async Task<IActionResult> SetComponents(
        Guid id,
        [FromBody] IList<ComponentInput> components)
    {
        await _mediator.Send(new SetCompositeItemCommand(id, components));
        return NoContent();
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] StockMovementType? type,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
        => Ok(await _mediator.Send(new GetInventoryHistoryQuery(from, to, type, page, pageSize)));

    [HttpGet("valuation")]
    public async Task<IActionResult> GetValuation()
        => Ok(await _mediator.Send(new GetInventoryValuationQuery()));
}