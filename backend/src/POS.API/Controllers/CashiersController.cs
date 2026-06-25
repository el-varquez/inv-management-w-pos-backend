using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Cashiers.Commands.CreateCashier;
using POS.Application.Cashiers.Commands.DeactivateCashier;
using POS.Application.Cashiers.Commands.ReactivateCashier;
using POS.Application.Cashiers.Commands.ResetCashierPassword;
using POS.Application.Cashiers.Queries.GetCashiers;

namespace POS.API.Controllers;

[ApiController]
[Route("api/cashiers")]
[Authorize(Roles = "Admin")]
public class CashiersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CashiersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _mediator.Send(new GetCashiersQuery()));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCashierCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id }, new { id });
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _mediator.Send(new DeactivateCashierCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        await _mediator.Send(new ReactivateCashierCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetCashierPasswordCommand command)
    {
        await _mediator.Send(command with { Id = id });
        return NoContent();
    }
}
