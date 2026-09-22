using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Purchases;
using SimpleStore.Domain.Purchases;
using SimpleStore.Infrastructure.Identity;
using SimpleStore.Application.Corrections;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Owner)]
[Route("api/purchases")]
public sealed class PurchasesController(
    CreatePurchaseUseCase createPurchase,
    UpdatePurchaseUseCase updatePurchase,
    GetPurchaseUseCase getPurchase,
    GetPurchasesUseCase getPurchases,
    CompletePurchaseUseCase completePurchase,
    VoidPurchaseUseCase voidPurchase) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PurchaseListResult>> List(
        [FromQuery] PurchaseStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await getPurchases.ExecuteAsync(status, page, pageSize, cancellationToken));

    [HttpGet("{purchaseId:guid}")]
    public async Task<ActionResult<PurchaseResult>> Get(
        Guid purchaseId,
        CancellationToken cancellationToken) =>
        Ok(await getPurchase.ExecuteAsync(purchaseId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PurchaseResult>> Create(
        PurchaseWriteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createPurchase.ExecuteAsync(request.ToCommand(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { purchaseId = result.Id }, result);
    }

    [HttpPut("{purchaseId:guid}")]
    public async Task<ActionResult<PurchaseResult>> Update(
        Guid purchaseId,
        PurchaseWriteRequest request,
        CancellationToken cancellationToken) =>
        Ok(await updatePurchase.ExecuteAsync(purchaseId, request.ToCommand(), cancellationToken));

    [HttpPost("{purchaseId:guid}/complete")]
    public async Task<ActionResult<PurchaseResult>> Complete(
        Guid purchaseId,
        CompletePurchaseRequest request,
        CancellationToken cancellationToken) =>
        Ok(await completePurchase.ExecuteAsync(purchaseId, request.ToCommand(), cancellationToken));

    [HttpPost("{purchaseId:guid}/void")]
    public async Task<ActionResult<PurchaseVoidResult>> Void(
        Guid purchaseId,
        VoidPurchaseRequest request,
        CancellationToken cancellationToken) =>
        Ok(await voidPurchase.ExecuteAsync(
            purchaseId,
            new VoidTransactionCommand(request.OperationId, request.Reason),
            cancellationToken));
}

public sealed record PurchaseLineRequest(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record PurchaseWriteRequest(
    Guid SupplierId,
    IReadOnlyCollection<PurchaseLineRequest> Lines)
{
    public PurchaseWriteCommand ToCommand() =>
        new(
            SupplierId,
            Lines.Select(line => new PurchaseLineCommand(
                line.ProductId,
                line.Quantity,
                line.UnitPrice)).ToArray());
}

public sealed record PurchasePaymentRequest(decimal Amount, string Method);

public sealed record CompletePurchaseRequest(
    Guid OperationId,
    IReadOnlyCollection<PurchasePaymentRequest> Payments)
{
    public CompletePurchaseCommand ToCommand() =>
        new(
            OperationId,
            Payments.Select(payment => new PurchasePaymentCommand(
                payment.Amount,
                payment.Method)).ToArray());
}

public sealed record VoidPurchaseRequest(Guid OperationId, string Reason);
