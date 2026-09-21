using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Customers;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{ApplicationRoles.Owner},{ApplicationRoles.Cashier}")]
[Route("api/customers")]
public sealed class CustomersController(
    CreateCustomerUseCase createCustomer,
    GetCustomerUseCase getCustomer,
    GetCustomersUseCase getCustomers) : ControllerBase
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
