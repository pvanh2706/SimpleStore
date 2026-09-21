using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Errors;
using SimpleStore.Application.ProductImports;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Owner)]
[Route("api/product-imports")]
public sealed class ProductImportsController(
    ValidateProductImportUseCase validateImport,
    ConfirmProductImportUseCase confirmImport) : ControllerBase
{
    private const int MaximumFileSize = 5 * 1024 * 1024;
    private const string CsvHeader = "SKU,Barcode,Name,Unit,SalePrice,OpeningCost,OpeningQuantity\r\n";

    [HttpGet("template")]
    public IActionResult Template()
    {
        var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(CsvHeader)).ToArray();
        return File(content, "text/csv; charset=utf-8", "simplestore-products.csv");
    }

    [HttpPost("validate")]
    [RequestSizeLimit(MaximumFileSize)]
    public async Task<ActionResult<ProductImportValidationResult>> Validate(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length is <= 0 or > MaximumFileSize
            || !Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new ApplicationValidationException(
                "invalid-import-file",
                "Choose a non-empty CSV file no larger than 5 MB.",
                [new ValidationError(null, "file", "invalid-import-file", "Choose a non-empty CSV file no larger than 5 MB.")]);
        }

        await using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);
        return Ok(await validateImport.ExecuteAsync(stream.ToArray(), cancellationToken));
    }

    [HttpPost("{importId:guid}/confirm")]
    public async Task<ActionResult<ProductImportConfirmResult>> Confirm(
        Guid importId,
        CancellationToken cancellationToken) =>
        Ok(await confirmImport.ExecuteAsync(importId, cancellationToken));
}
