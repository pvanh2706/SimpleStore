using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Stores;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Owner)]
[Route("api/store")]
public sealed class StoreController(
    InitializeStoreUseCase initializeStore,
    GetCurrentStoreUseCase getCurrentStore) : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<StoreResult?>> Current(CancellationToken cancellationToken) =>
        Ok(await getCurrentStore.ExecuteAsync(cancellationToken));

    [HttpPost("initialize")]
    public async Task<ActionResult<StoreResult>> Initialize(
        InitializeStoreRequest request,
        CancellationToken cancellationToken) =>
        Ok(await initializeStore.ExecuteAsync(request.Name, cancellationToken));
}

public sealed record InitializeStoreRequest(string Name);
