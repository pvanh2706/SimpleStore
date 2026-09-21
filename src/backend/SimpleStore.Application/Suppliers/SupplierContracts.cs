namespace SimpleStore.Application.Suppliers;

public sealed record SupplierWriteCommand(string Name, string? Phone, string? Note);

public sealed record SupplierResult(
    Guid Id,
    string Name,
    string? Phone,
    string? Note,
    bool IsActive,
    decimal OutstandingAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SupplierListResult(
    IReadOnlyList<SupplierResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
