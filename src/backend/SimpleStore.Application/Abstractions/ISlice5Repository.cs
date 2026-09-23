using SimpleStore.Domain.Debts;
using SimpleStore.Domain.Operations;

namespace SimpleStore.Application.Abstractions;

public interface ISlice5Repository
{
    Task<DebtPartyBalance?> GetCurrentCustomerDebtAsync(
        Guid storeId,
        Guid customerId,
        CancellationToken cancellationToken);

    Task<DebtPartyBalance?> GetCustomerDebtAsOfAsync(
        Guid storeId,
        Guid customerId,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken);

    Task<DebtPartyBalance?> GetCurrentSupplierDebtAsync(
        Guid storeId,
        Guid supplierId,
        CancellationToken cancellationToken);

    Task<DebtPartyBalance?> GetSupplierDebtAsOfAsync(
        Guid storeId,
        Guid supplierId,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken);

    Task<DebtPartyBalancePage> SearchCurrentCustomerDebtsAsync(
        Guid storeId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<DebtPartyBalancePage> SearchCurrentSupplierDebtsAsync(
        Guid storeId,
        string? search,
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
