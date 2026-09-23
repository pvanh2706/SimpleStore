using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Stores;
using SimpleStore.Domain.Stores;

namespace SimpleStore.Application.Reports;

public sealed class GetEndOfDayReportUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice5BRepository repository)
{
    public async Task<EndOfDayReportResult> ExecuteAsync(
        DateOnly businessDate,
        CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(
            currentUser, slice1Repository, cancellationToken);
        var store = await slice1Repository.GetStoreAsync(storeId, cancellationToken)
            ?? throw new ApplicationNotFoundException("store-not-found", "Store was not found.");
        var window = BusinessDateWindow.Resolve(businessDate, store.TimeZoneId);
        var data = await repository.GetEndOfDayAsync(
            storeId, window.StartUtc, window.EndUtc, cancellationToken);

        EnsureNonnegative(data.EndingCustomerDebt, "customer-debt-state-invalid");
        EnsureNonnegative(data.EndingSupplierDebt, "supplier-debt-state-invalid");

        var financial = DailyFinancialProjection.Project(data);
        var purchasePayments = data.PurchasePaymentsCash + data.PurchasePaymentsTransfer;
        var supplierDebtPayments = data.SupplierDebtPaymentsCash + data.SupplierDebtPaymentsTransfer;

        return new EndOfDayReportResult(
            businessDate,
            window.TimeZoneId,
            window.StartUtc,
            window.EndUtc,
            financial.SalesRevenue,
            financial.Collected,
            data.EndingCustomerDebt,
            new SupplierPaymentsResult(
                purchasePayments,
                supplierDebtPayments,
                purchasePayments + supplierDebtPayments),
            data.EndingSupplierDebt,
            financial.EstimatedGrossProfit);
    }

    private static void EnsureNonnegative(decimal value, string code)
    {
        if (value < 0)
        {
            throw new ApplicationConflictException(
                code, "Debt history produces a negative outstanding balance.");
        }
    }
}
