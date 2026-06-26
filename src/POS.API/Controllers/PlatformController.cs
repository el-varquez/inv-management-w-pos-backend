using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Platform.Commands.CreateTenant;
using POS.Application.Platform.Commands.ReactivateTenant;
using POS.Application.Platform.Commands.SetCashierCap;
using POS.Application.Platform.Commands.SuspendTenant;
using POS.Application.Platform.Queries.GetTenant;
using POS.Application.Platform.Queries.GetTenants;

namespace POS.API.Controllers;

[ApiController]
[Route("api/platform/tenants")]
[Authorize(Roles = "SuperAdmin")]
public class PlatformController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _mediator.Send(new GetTenantsQuery()));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne(Guid id)
        => Ok(await _mediator.Send(new GetTenantQuery(id)));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetOne), new { id }, new { id });
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id)
    {
        await _mediator.Send(new SuspendTenantCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        await _mediator.Send(new ReactivateTenantCommand(id));
        return NoContent();
    }

    [HttpPatch("{id:guid}/cashier-cap")]
    public async Task<IActionResult> SetCashierCap(Guid id, [FromBody] SetCashierCapCommand command)
    {
        await _mediator.Send(command with { Id = id });
        return NoContent();
    }
}
