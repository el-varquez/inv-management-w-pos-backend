using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Reports.Queries.GetExpenseReport;
using POS.Application.Reports.Queries.GetProfitReport;
using POS.Application.Reports.Queries.GetSalesReport;

namespace POS.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => Ok(await _mediator.Send(new GetSalesReportQuery(from, to)));

    [HttpGet("expenses")]
    public async Task<IActionResult> GetExpenseReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => Ok(await _mediator.Send(new GetExpenseReportQuery(from, to)));

    [HttpGet("profit")]
    public async Task<IActionResult> GetProfitReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? itemId)
        => Ok(await _mediator.Send(new GetProfitReportQuery(from, to, categoryId, itemId)));
}
