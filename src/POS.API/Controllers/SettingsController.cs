using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Settings.Commands.UpdateStoreSettings;
using POS.Application.Settings.Queries.GetStoreName;
using POS.Application.Settings.Queries.GetStoreSettings;

namespace POS.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IMediator _mediator;
    public SettingsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await _mediator.Send(new GetStoreSettingsQuery()));

    [HttpGet("store-name")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStoreName()
        => Ok(await _mediator.Send(new GetStoreNameQuery()));

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromBody] UpdateStoreSettingsCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }
}
