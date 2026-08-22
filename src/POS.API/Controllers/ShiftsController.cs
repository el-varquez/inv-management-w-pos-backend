using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Shifts.Commands.CloseShift;
using POS.Application.Shifts.Commands.CorrectShiftCount;
using POS.Application.Shifts.Commands.OpenShift;
using POS.Application.Shifts.Commands.RecordDrawerMovement;
using POS.Application.Shifts.Commands.RecordEWalletTransaction;
using POS.Application.Shifts.Commands.UpdateStartingCash;
using POS.Application.Shifts.Commands.VoidDrawerMovement;
using POS.Application.Shifts.Commands.VoidEWalletTransaction;
using POS.Application.Shifts.Queries.GetCurrentShift;
using POS.Application.Shifts.Queries.GetShiftRead;
using POS.Application.Shifts.Queries.GetShifts;

namespace POS.API.Controllers;

[ApiController]
[Route("api/shifts")]
[Authorize]
public class ShiftsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ShiftsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        => Ok(await _mediator.Send(new GetShiftsQuery(page, pageSize)));

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent()
        => Ok(await _mediator.Send(new GetCurrentShiftQuery()));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
        => Ok(await _mediator.Send(new GetShiftReadQuery(id)));

    [HttpPost("open")]
    public async Task<IActionResult> Open([FromBody] OpenShiftCommand command)
        => Ok(new ShiftIdResponse(await _mediator.Send(command)));

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseShiftRequest body)
    {
        await _mediator.Send(new CloseShiftCommand(id, body.CountedCash, body.CountedEWalletBalance));
        return NoContent();
    }

    [HttpPost("movements")]
    public async Task<IActionResult> RecordMovement([FromBody] RecordDrawerMovementCommand command)
        => Ok(new ShiftIdResponse(await _mediator.Send(command)));

    [HttpPost("ewallet")]
    public async Task<IActionResult> RecordEWallet(
        [FromBody] RecordEWalletTransactionCommand command)
        => Ok(new ShiftIdResponse(await _mediator.Send(command)));

    [HttpPost("ewallet/{id:guid}/void")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> VoidEWallet(Guid id)
    {
        await _mediator.Send(new VoidEWalletTransactionCommand(id));
        return NoContent();
    }

    [HttpPost("movements/{id:guid}/void")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> VoidMovement(Guid id)
    {
        await _mediator.Send(new VoidDrawerMovementCommand(id));
        return NoContent();
    }

    [HttpPut("{id:guid}/starting-cash")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStartingCash(
        Guid id, [FromBody] StartingCashRequest body)
    {
        await _mediator.Send(new UpdateStartingCashCommand(id, body.StartingCash, body.Reason));
        return NoContent();
    }

    [HttpPost("{id:guid}/correct-count")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CorrectCount(
        Guid id, [FromBody] CorrectCountRequest body)
    {
        await _mediator.Send(new CorrectShiftCountCommand(id, body.CountedCash, body.Reason));
        return NoContent();
    }
}

public record CorrectCountRequest(decimal CountedCash, string Reason);
public record CloseShiftRequest(decimal CountedCash, decimal? CountedEWalletBalance);
public record StartingCashRequest(decimal StartingCash, string Reason);
public record ShiftIdResponse(Guid Id);
