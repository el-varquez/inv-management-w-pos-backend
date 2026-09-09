using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Invoices.Commands.CreateInvoice;
using POS.Application.Invoices.Commands.VoidInvoice;
using POS.Application.Invoices.Queries.GetInvoiceById;
using POS.Application.Invoices.Queries.GetInvoices;

namespace POS.API.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceCommand command)
        => Ok(await _mediator.Send(command));

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
        => Ok(await _mediator.Send(new GetInvoicesQuery(from, to, page, pageSize)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _mediator.Send(new GetInvoiceByIdQuery(id)));

    [HttpPost("{id:guid}/void")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Void(Guid id)
    {
        await _mediator.Send(new VoidInvoiceCommand(id));
        return NoContent();
    }
}
