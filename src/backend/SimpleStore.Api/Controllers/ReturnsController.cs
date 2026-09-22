using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Returns;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Owner)]
[Route("api/returns")]
public sealed class ReturnsController(
    PreviewReturnUseCase previewReturn,
    CreateReturnUseCase createReturn,
    GetReturnUseCase getReturn) : ControllerBase
{
    [HttpPost("preview")]
    public async Task<ActionResult<ReturnPreviewResult>> Preview(
        ReturnPreviewRequest request,
        CancellationToken cancellationToken) =>
        Ok(await previewReturn.ExecuteAsync(request.ToCommand(), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ReturnResult>> Create(
        CreateReturnRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createReturn.ExecuteAsync(request.ToCommand(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{returnId:guid}")]
    public async Task<ActionResult<ReturnResult>> Get(Guid returnId, CancellationToken cancellationToken) =>
        Ok(await getReturn.ExecuteAsync(returnId, cancellationToken));
}

public sealed record ReturnLineRequest(Guid OriginalSaleLineId, decimal Quantity, bool? Restock);
public sealed record ReturnPreviewRequest(Guid OriginalSaleId, IReadOnlyCollection<ReturnLineRequest> Lines)
{
    public ReturnPreviewCommand ToCommand() => new(
        OriginalSaleId,
        Lines.Select(item => new ReturnLineCommand(item.OriginalSaleLineId, item.Quantity, item.Restock)).ToArray());
}

public sealed record CreateReturnRequest(
    Guid OperationId,
    Guid OriginalSaleId,
    IReadOnlyCollection<ReturnLineRequest> Lines,
    string? RefundMethod)
{
    public CreateReturnCommand ToCommand() => new(
        OperationId,
        OriginalSaleId,
        Lines.Select(item => new ReturnLineCommand(item.OriginalSaleLineId, item.Quantity, item.Restock)).ToArray(),
        RefundMethod);
}
