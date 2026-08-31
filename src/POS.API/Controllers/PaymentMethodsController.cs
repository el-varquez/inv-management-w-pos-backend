using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.PaymentMethods.Commands.CreatePaymentMethod;
using POS.Application.PaymentMethods.Commands.UpdatePaymentMethod;
using POS.Application.PaymentMethods.Queries.GetPaymentMethods;

namespace POS.API.Controllers;

[ApiController]
[Route("api/payment-methods")]
[Authorize]
public class PaymentMethodsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentMethodsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _mediator.Send(new GetPaymentMethodsQuery()));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreatePaymentMethodCommand command)
        => Ok(await _mediator.Send(command));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePaymentMethodCommand command)
    {
        await _mediator.Send(command with { Id = id });
        return NoContent();
    }
}
