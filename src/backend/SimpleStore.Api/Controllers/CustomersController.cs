using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Customers;
using SimpleStore.Application.Debts;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{ApplicationRoles.Owner},{ApplicationRoles.Cashier}")]
[Route("api/customers")]
public sealed class CustomersController(
    CreateCustomerUseCase createCustomer,
    GetCustomerUseCase getCustomer,
    GetCustomersUseCase getCustomers,
    GetCustomerDebtUseCase getCustomerDebt,
    GetCustomerDebtsUseCase getCustomerDebts,
    RecordCustomerDebtPaymentUseCase recordCustomerDebtPayment) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CustomerListResult>> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await getCustomers.ExecuteAsync(search, page, pageSize, cancellationToken));

    [HttpGet("{customerId:guid}")]
    public async Task<ActionResult<CustomerResult>> Get(
        Guid customerId,
        CancellationToken cancellationToken) =>
        Ok(await getCustomer.ExecuteAsync(customerId, cancellationToken));

    [HttpGet("debts")]
    public async Task<ActionResult<DebtBalanceListResult>> ListDebts(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await getCustomerDebts.ExecuteAsync(search, page, pageSize, cancellationToken));

    [HttpGet("{customerId:guid}/debt")]
    public async Task<ActionResult<DebtBalanceResult>> GetDebt(
        Guid customerId,
        CancellationToken cancellationToken) =>
        Ok(await getCustomerDebt.ExecuteAsync(customerId, cancellationToken));

    [HttpPost("{customerId:guid}/debt-payments")]
    public async Task<ActionResult<DebtPaymentResult>> RecordDebtPayment(
        Guid customerId,
        DebtPaymentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await recordCustomerDebtPayment.ExecuteAsync(customerId, request.ToCommand(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CustomerResult>> Create(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createCustomer.ExecuteAsync(
            new CreateCustomerCommand(request.Name, request.Phone),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { customerId = result.Id }, result);
    }
}

public sealed record CreateCustomerRequest(string Name, string? Phone);

public sealed record DebtPaymentRequest(
    Guid OperationId,
    decimal Amount,
    string Method,
    decimal ExpectedOutstandingAmount,
    string? Note)
{
    public DebtPaymentCommand ToCommand() =>
        new(OperationId, Amount, Method, ExpectedOutstandingAmount, Note);
}
