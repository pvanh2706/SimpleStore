using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Reports;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Owner)]
[Route("api/today")]
public sealed class TodayController(
    GetTodaySummaryUseCase getSummary,
    GetTodayExplanationUseCase getExplanation) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TodaySummaryResult>> Summary(
        CancellationToken cancellationToken) =>
        Ok(await getSummary.ExecuteAsync(cancellationToken));

    [HttpGet("explanations/{metric}")]
    public async Task<ActionResult<TodayExplanationResult>> Explanation(
        string metric,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await getExplanation.ExecuteAsync(metric, page, pageSize, cancellationToken));
}
