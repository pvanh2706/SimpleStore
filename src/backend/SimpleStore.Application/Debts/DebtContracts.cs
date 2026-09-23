namespace SimpleStore.Application.Debts;

public sealed record DebtPaymentCommand(
    Guid OperationId,
    decimal Amount,
    string Method,
    decimal ExpectedOutstandingAmount,
    string? Note);

public sealed record DebtBalanceResult(
    Guid PartyId,
    string PartyName,
    string? Phone,
    decimal OutstandingAmount,
    DateTimeOffset AsOf);

public sealed record DebtBalanceListResult(
    IReadOnlyList<DebtBalanceResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    DateTimeOffset AsOf);

public sealed record DebtPaymentResult(
    Guid Id,
    Guid PartyId,
    string Direction,
    string Purpose,
    decimal Amount,
    string Method,
    string? Note,
    DateTimeOffset OccurredAt,
    Guid PerformedByUserId,
    decimal OutstandingBefore,
    decimal OutstandingAfter,
    bool WasAlreadyRecorded = false);
