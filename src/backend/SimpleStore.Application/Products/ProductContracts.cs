namespace SimpleStore.Application.Products;

public sealed record ProductWriteCommand(
    string? Sku,
    string? Barcode,
    string Name,
    string Unit,
    decimal SalePrice,
    decimal? ReferencePurchaseCost,
    decimal OpeningQuantity = 0,
    decimal? OpeningCost = null);

public sealed record ProductResult(
    Guid Id,
    string Sku,
    string? Barcode,
    string Name,
    string Unit,
    decimal SalePrice,
    decimal? ReferencePurchaseCost,
    bool IsActive,
    decimal QuantityOnHand,
    decimal InventoryValue,
    decimal AverageCost,
    bool HasAverageCost,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProductListResult(
    IReadOnlyList<ProductListItemResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ProductListItemResult(
    Guid Id,
    string Sku,
    string? Barcode,
    string Name,
    string Unit,
    decimal SalePrice,
    bool IsActive,
    decimal QuantityOnHand);

public sealed record InventoryBalanceResult(
    Guid ProductId,
    Guid WarehouseId,
    decimal QuantityOnHand,
    decimal InventoryValue,
    decimal AverageCost,
    bool HasAverageCost,
    DateTimeOffset UpdatedAt);

public sealed record InventoryMovementResult(
    Guid Id,
    string Type,
    decimal QuantityDelta,
    decimal InventoryValueDelta,
    decimal UnitCost,
    string SourceType,
    Guid SourceId,
    DateTimeOffset OccurredAt);
