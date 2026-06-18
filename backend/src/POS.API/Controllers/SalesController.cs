using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Sales.Commands.CreateTransaction;
using POS.Application.Sales.Commands.ProcessRefund;
using POS.Application.Sales.Queries.GetSalesSummary;
using POS.Application.Sales.Queries.GetTransactionById;
using POS.Application.Sales.Queries.GetTransactions;

namespace POS.API.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize] 
public class SalesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SalesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id)
    {
        var result = await _mediator.Send(new ProcessRefundCommand(id));
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? page,
        [FromQuery] int? pageSize)
        => Ok(await _mediator.Send(new GetTransactionsQuery(from, to, page, pageSize)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await _mediator.Send(new GetTransactionByIdQuery(id)));

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => Ok(await _mediator.Send(new GetSalesSummaryQuery(from, to)));
}