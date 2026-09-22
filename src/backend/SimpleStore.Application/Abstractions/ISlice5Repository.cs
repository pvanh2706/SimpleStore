using SimpleStore.Domain.Debts;
using SimpleStore.Domain.Operations;

namespace SimpleStore.Application.Abstractions;

public interface ISlice5Repository
{
    Task<DebtPartyBalance?> GetCustomerDebtAsync(
        Guid storeId,
        Guid customerId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken);

    Task<DebtPartyBalance?> GetSupplierDebtAsync(
        Guid storeId,
        Guid supplierId,
        DateTimeOffset asOf,
        CancellationToken cancellationToken);

    Task<DebtPartyBalancePage> SearchCustomerDebtsAsync(
        Guid storeId,
        string? search,
        DateTimeOffset asOf,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<DebtPartyBalancePage> SearchSupplierDebtsAsync(
        Guid storeId,
        string? search,
        DateTimeOffset asOf,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<DebtPayment?> GetDebtPaymentAsync(
        Guid storeId,
        Guid debtPaymentId,
        CancellationToken cancellationToken);

    Task<BusinessOperation?> GetOperationAsync(
        Guid storeId,
        Guid operationId,
        CancellationToken cancellationToken);

    Task AcquireOperationLockAsync(Guid operationId, CancellationToken cancellationToken);
    Task AcquireCustomerDebtLockAsync(Guid storeId, Guid customerId, CancellationToken cancellationToken);
    Task AcquireSupplierDebtLockAsync(Guid storeId, Guid supplierId, CancellationToken cancellationToken);
    void AddDebtPayment(DebtPayment payment);
    void AddBusinessOperation(BusinessOperation operation);
    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);
}

public sealed record DebtPartyBalance(Guid PartyId, string PartyName, decimal OutstandingAmount);

public sealed record DebtPartyBalancePage(
    IReadOnlyList<DebtPartyBalance> Items,
    int TotalCount);
