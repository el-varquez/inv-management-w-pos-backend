using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using POS.Application.Auth.Commands.Login;
using POS.Application.Auth.Commands.SetupPassword;
using POS.Application.Common.Interfaces;

namespace POS.API.Controller;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IJwtService _jwtService;

    public AuthController(IMediator mediator, IJwtService jwtService)
    {
        _mediator = mediator;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
        => Ok(ToResponse(await _mediator.Send(command)));

    [HttpPost("setup-password")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> SetupPassword([FromBody] SetupPasswordCommand command)
        => Ok(ToResponse(await _mediator.Send(command)));

    private LoginResponse ToResponse(LoginResult result)
    {
        var user = result.User!;
        if (result.PasswordSetupRequired)
            return new LoginResponse(null, user.Name, user.Username, user.Role, true);

        return new LoginResponse(
            _jwtService.GenerateToken(user), user.Name, user.Username, user.Role, false);
    }
}

public record LoginResponse(
    string? Token,
    string? Name,
    string? Username,
    string? Role,
    bool PasswordSetupRequired);
