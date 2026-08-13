using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Settings.Commands.UpdateStoreSettings;
using POS.Application.Settings.Queries.GetStoreSettings;

namespace POS.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Roles = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly IMediator _mediator;
    public SettingsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await _mediator.Send(new GetStoreSettingsQuery()));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateStoreSettingsCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }
}
