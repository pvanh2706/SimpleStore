using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Reports;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Owner)]
[Route("api/experiments/c14/events")]
public sealed class C14ExperimentEventsController(
    RecordC14ExperimentEventUseCase recordEvent) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<C14ExperimentEventResult>> Record(
        RecordC14ExperimentEventRequest request,
        CancellationToken cancellationToken) =>
        Ok(await recordEvent.ExecuteAsync(
            new RecordC14ExperimentEventCommand(
                request.EventId,
                request.EventType,
                request.ProductId,
                request.AttentionKind),
            cancellationToken));
}

public sealed record RecordC14ExperimentEventRequest(
    Guid EventId,
    string EventType,
    Guid? ProductId,
    string? AttentionKind);
