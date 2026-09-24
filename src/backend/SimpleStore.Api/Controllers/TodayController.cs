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
    GetTodayExplanationUseCase getExplanation,
    GetC14AttentionListUseCase getAttentionList,
    GetC14AttentionDetailUseCase getAttentionDetail) : ControllerBase
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

    [HttpGet("attention")]
    public async Task<ActionResult<C14AttentionListResult>> Attention(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await getAttentionList.ExecuteAsync(page, pageSize, cancellationToken));

    [HttpGet("attention/{productId:guid}")]
    public async Task<ActionResult<C14AttentionDetailResult>> AttentionDetail(
        Guid productId,
        CancellationToken cancellationToken) =>
        Ok(await getAttentionDetail.ExecuteAsync(productId, cancellationToken));
}
