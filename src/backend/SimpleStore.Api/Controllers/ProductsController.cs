using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Products;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController(
    CreateProductUseCase createProduct,
    UpdateProductUseCase updateProduct,
    DeactivateProductUseCase deactivateProduct,
    GetProductUseCase getProduct,
    GetProductsUseCase getProducts,
    GetInventoryBalanceUseCase getInventoryBalance,
    GetInventoryMovementsUseCase getInventoryMovements) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProductListResult>> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await getProducts.ExecuteAsync(search, isActive, page, pageSize, cancellationToken));

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ProductResult>> Get(
        Guid productId,
        CancellationToken cancellationToken) =>
        Ok(await getProduct.ExecuteAsync(productId, cancellationToken));

    [Authorize(Roles = ApplicationRoles.Owner)]
    [HttpPost]
    public async Task<ActionResult<ProductResult>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createProduct.ExecuteAsync(request.ToCommand(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { productId = result.Id }, result);
    }

    [Authorize(Roles = ApplicationRoles.Owner)]
    [HttpPut("{productId:guid}")]
    public async Task<ActionResult<ProductResult>> Update(
        Guid productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken) =>
        Ok(await updateProduct.ExecuteAsync(productId, request.ToCommand(), cancellationToken));

    [Authorize(Roles = ApplicationRoles.Owner)]
    [HttpPost("{productId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid productId,
        CancellationToken cancellationToken)
    {
        await deactivateProduct.ExecuteAsync(productId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{productId:guid}/inventory")]
    public async Task<ActionResult<InventoryBalanceResult>> Inventory(
        Guid productId,
        CancellationToken cancellationToken) =>
        Ok(await getInventoryBalance.ExecuteAsync(productId, cancellationToken));

    [HttpGet("{productId:guid}/movements")]
    public async Task<ActionResult<IReadOnlyList<InventoryMovementResult>>> Movements(
        Guid productId,
        CancellationToken cancellationToken) =>
        Ok(await getInventoryMovements.ExecuteAsync(productId, cancellationToken));
}

public sealed record CreateProductRequest(
    string? Sku,
    string? Barcode,
    string Name,
    string Unit,
    decimal SalePrice,
    decimal? ReferencePurchaseCost,
    decimal OpeningQuantity = 0,
    decimal? OpeningCost = null)
{
    public ProductWriteCommand ToCommand() =>
        new(
            Sku,
            Barcode,
            Name,
            Unit,
            SalePrice,
            ReferencePurchaseCost,
            OpeningQuantity,
            OpeningCost);
}

public sealed record UpdateProductRequest(
    string? Sku,
    string? Barcode,
    string Name,
    string Unit,
    decimal SalePrice,
    decimal? ReferencePurchaseCost)
{
    public ProductWriteCommand ToCommand() =>
        new(Sku, Barcode, Name, Unit, SalePrice, ReferencePurchaseCost);
}
