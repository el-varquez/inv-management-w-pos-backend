using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Shifts.Commands.CorrectShiftCount;
using POS.Application.Shifts.Queries.GetCurrentShift;
using POS.Application.Shifts.Queries.GetShiftRead;
using POS.Application.Shifts.Queries.GetShifts;

namespace POS.API.Controllers;

[ApiController]
[Route("api/shifts")]
[Authorize(Roles = "Admin")]
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

    [HttpPost("{id:guid}/correct-count")]
    public async Task<IActionResult> CorrectCount(
        Guid id, [FromBody] CorrectCountRequest body)
    {
        await _mediator.Send(new CorrectShiftCountCommand(id, body.CountedCash, body.Reason));
        return NoContent();
    }
}

public record CorrectCountRequest(decimal CountedCash, string Reason);
