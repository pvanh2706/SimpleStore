using SimpleStore.Domain.Inventory;

namespace SimpleStore.Application.Abstractions;

public interface ISlice5BRepository
{
    Task<EndOfDayData> GetEndOfDayAsync(
        Guid storeId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken);
}

public sealed record EndOfDayData(
    decimal CompletedSales,
    decimal CompletedReturns,
    decimal VoidedSales,
    decimal SalePaymentsCash,
    decimal SalePaymentsTransfer,
    decimal CustomerDebtPaymentsCash,
    decimal CustomerDebtPaymentsTransfer,
    decimal RefundsCash,
    decimal RefundsTransfer,
    decimal PurchasePaymentsCash,
    decimal PurchasePaymentsTransfer,
    decimal SupplierDebtPaymentsCash,
    decimal SupplierDebtPaymentsTransfer,
    decimal EndingCustomerDebt,
    decimal EndingSupplierDebt,
    decimal DirectSaleCogs,
    decimal RestockedReturnValue,
    decimal VoidedSaleCogs,
    CostReliability CostReliability);
