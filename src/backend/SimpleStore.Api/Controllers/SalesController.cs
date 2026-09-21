using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Sales;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{ApplicationRoles.Owner},{ApplicationRoles.Cashier}")]
[Route("api/sales")]
public sealed class SalesController(
    CompleteSaleUseCase completeSale,
    GetSaleUseCase getSale,
    GetSalesUseCase getSales) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SaleListResult>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await getSales.ExecuteAsync(page, pageSize, cancellationToken));

    [HttpGet("{saleId:guid}")]
    public async Task<ActionResult<SaleResult>> Get(
        Guid saleId,
        CancellationToken cancellationToken) =>
        Ok(await getSale.ExecuteAsync(saleId, cancellationToken));

    [HttpPost("complete")]
    public async Task<ActionResult<SaleResult>> Complete(
        CompleteSaleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await completeSale.ExecuteAsync(request.ToCommand(), cancellationToken));
}

public sealed record CompleteSaleLineRequest(Guid ProductId, decimal Quantity);
public sealed record SalePaymentRequest(decimal Amount, string Method);
public sealed record CompleteSaleRequest(
    Guid OperationId,
    Guid? CustomerId,
    IReadOnlyCollection<CompleteSaleLineRequest> Lines,
    IReadOnlyCollection<SalePaymentRequest> Payments)
{
    public CompleteSaleCommand ToCommand() =>
        new(
            OperationId,
            CustomerId,
            Lines.Select(line => new CompleteSaleLineCommand(line.ProductId, line.Quantity)).ToArray(),
            Payments.Select(payment => new SalePaymentCommand(payment.Amount, payment.Method)).ToArray());
}
