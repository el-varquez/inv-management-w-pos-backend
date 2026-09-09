using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Utang.Commands.CollectUtangPayment;
using POS.Application.Utang.Commands.CreateSuki;
using POS.Application.Utang.Commands.DeleteSuki;
using POS.Application.Utang.Commands.UpdateSuki;
using POS.Application.Utang.Commands.EditUtangPayment;
using POS.Application.Utang.Commands.VoidUtangPayment;
using POS.Application.Utang.Queries.GetSukiLedger;
using POS.Application.Utang.Queries.GetSukis;
using POS.Application.Utang.Queries.GetUtangOutstanding;
using POS.Application.Utang.Queries.GetUtangSummary;

namespace POS.API.Controllers;

[ApiController]
[Route("api/utang")]
[Authorize]
public class UtangController : ControllerBase
{
    private readonly IMediator _mediator;
    public UtangController(IMediator mediator) => _mediator = mediator;

    [HttpGet("sukis")]
    public async Task<IActionResult> GetSukis(
        [FromQuery] string? term, [FromQuery] int? page, [FromQuery] int? pageSize)
        => Ok(await _mediator.Send(new GetSukisQuery(term, page, pageSize)));

    [HttpPost("sukis")]
    public async Task<IActionResult> CreateSuki([FromBody] CreateSukiCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("sukis/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSuki(Guid id, [FromBody] SukiRequest body)
    {
        await _mediator.Send(new UpdateSukiCommand(id, body.Name, body.Phone));
        return NoContent();
    }

    [HttpDelete("sukis/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSuki(Guid id)
    {
        await _mediator.Send(new DeleteSukiCommand(id));
        return NoContent();
    }

    [HttpGet("sukis/{id:guid}/ledger")]
    public async Task<IActionResult> GetLedger(Guid id)
        => Ok(await _mediator.Send(new GetSukiLedgerQuery(id)));

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Ok(await _mediator.Send(new GetUtangSummaryQuery(from, to)));

    [HttpGet("outstanding")]
    public async Task<IActionResult> GetOutstanding()
        => Ok(await _mediator.Send(new GetUtangOutstandingQuery()));

    [HttpPost("collect")]
    public async Task<IActionResult> Collect(
        [FromBody] CollectUtangPaymentCommand command)
        => Ok(new EntryIdResponse(await _mediator.Send(command)));

    [HttpPost("payments/{id:guid}/void")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> VoidPayment(Guid id)
    {
        await _mediator.Send(new VoidUtangPaymentCommand(id));
        return NoContent();
    }

    [HttpPut("payments/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> EditPayment(
        Guid id, [FromBody] EditPaymentRequest body)
    {
        await _mediator.Send(new EditUtangPaymentCommand(id, body.Amount));
        return NoContent();
    }
}

public record SukiRequest(string Name, string? Phone);
public record EditPaymentRequest(decimal Amount);
public record EntryIdResponse(Guid Id);
