using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Purchases;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Owner)]
[Route("api/operations")]
public sealed class OperationsController(GetOperationStatusUseCase getOperationStatus) : ControllerBase
{
    [HttpGet("{operationId:guid}")]
    public async Task<ActionResult<OperationStatusResult>> Get(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var result = await getOperationStatus.ExecuteAsync(operationId, cancellationToken);
        return result is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Operation was not found.",
                type: "https://simplestore/errors/operation-not-found",
                extensions: new Dictionary<string, object?> { ["code"] = "operation-not-found" })
            : Ok(result);
    }
}
