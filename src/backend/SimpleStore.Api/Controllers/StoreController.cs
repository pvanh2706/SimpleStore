using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Stores;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{ApplicationRoles.Owner},{ApplicationRoles.Cashier}")]
[Route("api/store")]
public sealed class StoreController(
    InitializeStoreUseCase initializeStore,
    GetCurrentStoreUseCase getCurrentStore,
    GetStoreOperationalSettingsUseCase getOperationalSettings,
    UpdateNegativeStockPolicyUseCase updateNegativeStockPolicy) : ControllerBase
{
    [HttpGet("current")]
    public async Task<ActionResult<StoreResult?>> Current(CancellationToken cancellationToken) =>
        Ok(await getCurrentStore.ExecuteAsync(cancellationToken));

    [HttpPost("initialize")]
    [Authorize(Roles = ApplicationRoles.Owner)]
    public async Task<ActionResult<StoreResult>> Initialize(
        InitializeStoreRequest request,
        CancellationToken cancellationToken) =>
        Ok(await initializeStore.ExecuteAsync(request.Name, cancellationToken));

    [HttpGet("operational-settings")]
    public async Task<ActionResult<StoreOperationalSettingsResult>> OperationalSettings(
        CancellationToken cancellationToken) =>
        Ok(await getOperationalSettings.ExecuteAsync(cancellationToken));

    [HttpPut("operational-settings/negative-stock")]
    [Authorize(Roles = ApplicationRoles.Owner)]
    public async Task<ActionResult<StoreOperationalSettingsResult>> UpdateNegativeStock(
        UpdateNegativeStockRequest request,
        CancellationToken cancellationToken) =>
        Ok(await updateNegativeStockPolicy.ExecuteAsync(
            request.AllowNegativeStock,
            cancellationToken));
}

public sealed record InitializeStoreRequest(string Name);

public sealed record UpdateNegativeStockRequest(bool AllowNegativeStock);
