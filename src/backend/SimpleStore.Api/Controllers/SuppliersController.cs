using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Suppliers;
using SimpleStore.Application.Debts;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Owner)]
[Route("api/suppliers")]
public sealed class SuppliersController(
    CreateSupplierUseCase createSupplier,
    UpdateSupplierUseCase updateSupplier,
    DeactivateSupplierUseCase deactivateSupplier,
    GetSupplierUseCase getSupplier,
    GetSuppliersUseCase getSuppliers,
    GetSupplierDebtUseCase getSupplierDebt,
    GetSupplierDebtsUseCase getSupplierDebts,
    RecordSupplierDebtPaymentUseCase recordSupplierDebtPayment) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SupplierListResult>> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await getSuppliers.ExecuteAsync(search, isActive, page, pageSize, cancellationToken));

    [HttpGet("{supplierId:guid}")]
    public async Task<ActionResult<SupplierResult>> Get(
        Guid supplierId,
        CancellationToken cancellationToken) =>
        Ok(await getSupplier.ExecuteAsync(supplierId, cancellationToken));

    [HttpGet("debts")]
    public async Task<ActionResult<DebtBalanceListResult>> ListDebts(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await getSupplierDebts.ExecuteAsync(search, page, pageSize, cancellationToken));

    [HttpGet("{supplierId:guid}/debt")]
    public async Task<ActionResult<DebtBalanceResult>> GetDebt(
        Guid supplierId,
        CancellationToken cancellationToken) =>
        Ok(await getSupplierDebt.ExecuteAsync(supplierId, cancellationToken));

    [HttpPost("{supplierId:guid}/debt-payments")]
    public async Task<ActionResult<DebtPaymentResult>> RecordDebtPayment(
        Guid supplierId,
        DebtPaymentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await recordSupplierDebtPayment.ExecuteAsync(supplierId, request.ToCommand(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SupplierResult>> Create(
        SupplierWriteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createSupplier.ExecuteAsync(request.ToCommand(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { supplierId = result.Id }, result);
    }

    [HttpPut("{supplierId:guid}")]
    public async Task<ActionResult<SupplierResult>> Update(
        Guid supplierId,
        SupplierWriteRequest request,
        CancellationToken cancellationToken) =>
        Ok(await updateSupplier.ExecuteAsync(supplierId, request.ToCommand(), cancellationToken));

    [HttpPost("{supplierId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        await deactivateSupplier.ExecuteAsync(supplierId, cancellationToken);
        return NoContent();
    }
}

public sealed record SupplierWriteRequest(string Name, string? Phone, string? Note)
{
    public SupplierWriteCommand ToCommand() => new(Name, Phone, Note);
}
