using SimpleStore.Application.Errors;

namespace SimpleStore.Application.ProductImports;

public sealed record ProductImportPreviewRow(
    int RowNumber,
    string Sku,
    string? Barcode,
    string Name,
    string Unit,
    decimal SalePrice,
    decimal? OpeningCost,
    decimal OpeningQuantity);

public sealed record ProductImportValidationResult(
    Guid? ImportId,
    bool IsValid,
    IReadOnlyList<ProductImportPreviewRow> Rows,
    IReadOnlyList<ValidationError> Errors);

public sealed record ProductImportConfirmResult(
    Guid ImportId,
    int ImportedProductCount,
    bool WasAlreadyCompleted);
