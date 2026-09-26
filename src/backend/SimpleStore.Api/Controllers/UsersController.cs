using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Owner)]
[Route("api/users/cashiers")]
public sealed class UsersController(AccountManagementService accounts) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CashierAccountResult>>> List(
        CancellationToken cancellationToken) =>
        Ok(await accounts.ListCashiersAsync(CurrentUserId(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CashierCredentialResult>> Create(
        CreateCashierRequest request,
        CancellationToken cancellationToken) =>
        Ok(await accounts.CreateCashierAsync(CurrentUserId(), request.Email, cancellationToken));

    [HttpPost("{cashierId:guid}/disable")]
    public async Task<ActionResult<CashierAccountResult>> Disable(
        Guid cashierId,
        CancellationToken cancellationToken) =>
        Ok(await accounts.DisableCashierAsync(CurrentUserId(), cashierId, cancellationToken));

    [HttpPost("{cashierId:guid}/credentials/reset")]
    public async Task<ActionResult<CashierCredentialResult>> ResetCredential(
        Guid cashierId,
        CancellationToken cancellationToken) =>
        Ok(await accounts.ResetCredentialAsync(CurrentUserId(), cashierId, cancellationToken));

    private Guid CurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new InvalidOperationException("An authenticated Owner is required.");
}

public sealed record CreateCashierRequest(string Email);
