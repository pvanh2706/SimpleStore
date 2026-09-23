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

        var netRevenue = data.CompletedSales - data.CompletedReturns - data.VoidedSales;
        var salePayments = data.SalePaymentsCash + data.SalePaymentsTransfer;
        var customerDebtPayments = data.CustomerDebtPaymentsCash + data.CustomerDebtPaymentsTransfer;
        var refunds = data.RefundsCash + data.RefundsTransfer;
        var grossCollected = salePayments + customerDebtPayments;
        var purchasePayments = data.PurchasePaymentsCash + data.PurchasePaymentsTransfer;
        var supplierDebtPayments = data.SupplierDebtPaymentsCash + data.SupplierDebtPaymentsTransfer;
        var cogs = data.DirectSaleCogs - data.RestockedReturnValue - data.VoidedSaleCogs;

        return new EndOfDayReportResult(
            businessDate,
            window.TimeZoneId,
            window.StartUtc,
            window.EndUtc,
            netRevenue,
            new CollectedResult(
                salePayments,
                customerDebtPayments,
                refunds,
                grossCollected - refunds),
            data.EndingCustomerDebt,
            new SupplierPaymentsResult(
                purchasePayments,
                supplierDebtPayments,
                purchasePayments + supplierDebtPayments),
            data.EndingSupplierDebt,
            new EstimatedGrossProfitResult(
                netRevenue,
                cogs,
                netRevenue - cogs,
                data.CostReliability.ToString()));
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
