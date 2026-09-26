using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Inventory;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Owner)]
[Route("api/inventory")]
public sealed class InventoryController(
    CreateStockAdjustmentUseCase createAdjustment,
    GetStocktakeContextUseCase getStocktakeContext,
    SubmitStocktakeUseCase submitStocktake) : ControllerBase
{
    [HttpPost("adjustments")]
    public async Task<ActionResult<StockAdjustmentResult>> Adjust(
        CreateStockAdjustmentCommand command,
        CancellationToken cancellationToken) =>
        Ok(await createAdjustment.ExecuteAsync(command, cancellationToken));

    [HttpGet("stocktakes/context/{productId:guid}")]
    public async Task<ActionResult<StocktakeContextResult>> StocktakeContext(
        Guid productId,
        CancellationToken cancellationToken) =>
        Ok(await getStocktakeContext.ExecuteAsync(productId, cancellationToken));

    [HttpPost("stocktakes")]
    public async Task<ActionResult<StocktakeResultDto>> Stocktake(
        SubmitStocktakeCommand command,
        CancellationToken cancellationToken) =>
        Ok(await submitStocktake.ExecuteAsync(command, cancellationToken));
}
