using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Platform.Commands.CreateTenant;
using POS.Application.Platform.Commands.DeactivateTenantUser;
using POS.Application.Platform.Commands.EditTenantUser;
using POS.Application.Platform.Commands.ReactivateTenant;
using POS.Application.Platform.Commands.ReactivateTenantUser;
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

    [HttpPut("{tenantId:guid}/users/{userId:guid}")]
    public async Task<IActionResult> EditUser(Guid tenantId, Guid userId, [FromBody] EditTenantUserCommand command)
    {
        await _mediator.Send(command with { TenantId = tenantId, UserId = userId });
        return NoContent();
    }

    [HttpPost("{tenantId:guid}/users/{userId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid tenantId, Guid userId)
    {
        await _mediator.Send(new DeactivateTenantUserCommand(tenantId, userId));
        return NoContent();
    }

    [HttpPost("{tenantId:guid}/users/{userId:guid}/reactivate")]
    public async Task<IActionResult> ReactivateUser(Guid tenantId, Guid userId)
    {
        await _mediator.Send(new ReactivateTenantUserCommand(tenantId, userId));
        return NoContent();
    }
}
