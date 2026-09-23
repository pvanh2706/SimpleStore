using SimpleStore.Application.Abstractions;

namespace SimpleStore.Application.Reports;

public sealed record DailyFinancialResult(
    decimal SalesRevenue,
    CollectedResult Collected,
    EstimatedGrossProfitResult EstimatedGrossProfit);

public static class DailyFinancialProjection
{
    public static DailyFinancialResult Project(EndOfDayData data)
    {
        var netRevenue = data.CompletedSales - data.CompletedReturns - data.VoidedSales;
        var salePayments = data.SalePaymentsCash + data.SalePaymentsTransfer;
        var customerDebtPayments = data.CustomerDebtPaymentsCash + data.CustomerDebtPaymentsTransfer;
        var refunds = data.RefundsCash + data.RefundsTransfer;
        var cogs = data.DirectSaleCogs - data.RestockedReturnValue - data.VoidedSaleCogs;

        return new DailyFinancialResult(
            netRevenue,
            new CollectedResult(
                salePayments,
                customerDebtPayments,
                refunds,
                salePayments + customerDebtPayments - refunds),
            new EstimatedGrossProfitResult(
                netRevenue,
                cogs,
                netRevenue - cogs,
                data.CostReliability.ToString()));
    }
}
