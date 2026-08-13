using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Dashboard.Queries.GetDashboardSummary;
using POS.Application.Dashboard.Queries.GetSalesTrend;

namespace POS.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
        => Ok(await _mediator.Send(new GetDashboardSummaryQuery()));

    [HttpGet("sales-trend")]
    public async Task<IActionResult> GetSalesTrend([FromQuery] string period = "week")
        => Ok(await _mediator.Send(new GetSalesTrendQuery(period)));
}
