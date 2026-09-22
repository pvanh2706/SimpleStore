namespace SimpleStore.Domain.Debts;

public static class DebtCalculations
{
    public static decimal CustomerOutstanding(
        decimal activeSaleObligations,
        decimal salePayments,
        decimal returnObligationReductions,
        decimal returnRefunds,
        decimal customerDebtPayments) =>
        activeSaleObligations
        - salePayments
        - returnObligationReductions
        + returnRefunds
        - customerDebtPayments;

    public static decimal SupplierOutstanding(
        decimal activePurchaseObligations,
        decimal purchasePayments,
        decimal supplierDebtPayments) =>
        activePurchaseObligations - purchasePayments - supplierDebtPayments;

    public static AggregateReturnFinancials CalculateAggregateReturn(
        decimal returnObligationReduction,
        decimal currentAggregateCustomerDebt)
    {
        if (returnObligationReduction < 0 || currentAggregateCustomerDebt < 0)
        {
            throw new DomainRuleException(
                "return-financial-state-invalid",
                "Return or aggregate customer debt state is invalid.");
        }

        var debtReduction = Math.Min(returnObligationReduction, currentAggregateCustomerDebt);
        return new AggregateReturnFinancials(
            returnObligationReduction,
            currentAggregateCustomerDebt,
            debtReduction,
            returnObligationReduction - debtReduction,
            currentAggregateCustomerDebt - debtReduction);
    }
}

public sealed record AggregateReturnFinancials(
    decimal ReturnObligationReduction,
    decimal CurrentAggregateCustomerDebt,
    decimal DebtReduction,
    decimal RequiredActualRefund,
    decimal EndingCustomerDebt);
