namespace SimpleStore.Application.Customers;

public sealed record CreateCustomerCommand(string Name, string? Phone);

public sealed record CustomerResult(
    Guid Id,
    string Name,
    string? Phone,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CustomerListResult(
    IReadOnlyList<CustomerResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
