using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Days.Commands.CloseDay;
using POS.Application.Days.Queries.GetCurrentDay;
using POS.Application.Days.Queries.GetDayRead;
using POS.Application.Days.Queries.GetDays;

namespace POS.API.Controllers;

[ApiController]
[Route("api/days")]
[Authorize(Roles = "Admin")]
public class DaysController : ControllerBase
{
    private readonly IMediator _mediator;
    public DaysController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize)
        => Ok(await _mediator.Send(new GetDaysQuery(page, pageSize)));

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent()
    {
        var day = await _mediator.Send(new GetCurrentDayQuery());
        return day is null ? NoContent() : Ok(day);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
        => Ok(await _mediator.Send(new GetDayReadQuery(id)));

    [HttpPost("close")]
    public async Task<IActionResult> Close()
    {
        await _mediator.Send(new CloseDayCommand());
        return NoContent();
    }
}
