namespace SimpleStore.Domain.Returns;

public static class ReturnCalculations
{
    public static ReturnLineAmounts CalculateLine(
        decimal originalQuantity,
        decimal originalLineAmount,
        decimal originalUnitSalePrice,
        decimal originalIssuedInventoryValue,
        decimal originalUnitCost,
        decimal previouslyReturnedQuantity,
        decimal previouslyReturnedFinancialValue,
        decimal previouslyRestockedQuantity,
        decimal previouslyRestockedValue,
        decimal requestedQuantity,
        bool restock)
    {
        var remainingQuantity = originalQuantity - previouslyReturnedQuantity;
        var remainingFinancialValue = originalLineAmount - previouslyReturnedFinancialValue;
        var remainingIssuedValue = originalIssuedInventoryValue - previouslyRestockedValue;
        if (remainingQuantity < 0 || remainingFinancialValue < 0 || remainingIssuedValue < 0)
        {
            throw new DomainRuleException("return-financial-state-invalid", "Existing return state is inconsistent.");
        }

        if (requestedQuantity <= 0 || decimal.Round(requestedQuantity, 3) != requestedQuantity)
        {
            throw new DomainRuleException("invalid-return-quantity", "Return quantity is invalid.");
        }

        if (requestedQuantity > remainingQuantity)
        {
            throw new DomainRuleException("return-quantity-exceeds-remaining", "Return quantity exceeds the remaining quantity.");
        }

        var candidateAmount = Math.Round(requestedQuantity * originalUnitSalePrice, 2, MidpointRounding.AwayFromZero);
        var returnAmount = requestedQuantity == remainingQuantity
            ? remainingFinancialValue
            : Math.Min(candidateAmount, remainingFinancialValue);
        decimal restockedValue = 0;
        if (restock)
        {
            var candidateInventoryValue = Math.Round(requestedQuantity * originalUnitCost, 2, MidpointRounding.AwayFromZero);
            restockedValue = previouslyRestockedQuantity + requestedQuantity == originalQuantity
                ? remainingIssuedValue
                : Math.Min(candidateInventoryValue, remainingIssuedValue);
        }

        return new ReturnLineAmounts(returnAmount, restockedValue, remainingQuantity);
    }

    public static ReturnFinancialAmounts CalculateFinancials(
        decimal originalTotal,
        decimal originalCollected,
        decimal previousReturnedValue,
        decimal previousRefunds,
        decimal currentReturnValue)
    {
        var cumulativeReturned = previousReturnedValue + currentReturnValue;
        var netObligation = originalTotal - cumulativeReturned;
        var cashBefore = originalCollected - previousRefunds;
        if (netObligation < 0 || cashBefore < 0)
        {
            throw new DomainRuleException("return-financial-state-invalid", "Existing return financial state is inconsistent.");
        }

        var refundDue = Math.Max(0, cashBefore - netObligation);
        var netCashHeld = cashBefore - refundDue;
        var outstanding = Math.Max(0, netObligation - netCashHeld);
        return new ReturnFinancialAmounts(cumulativeReturned, netObligation, netCashHeld, outstanding, refundDue);
    }
}

public sealed record ReturnLineAmounts(decimal ReturnLineAmount, decimal RestockedInventoryValue, decimal RemainingQuantityBefore);
public sealed record ReturnFinancialAmounts(
    decimal CumulativeReturnedValue,
    decimal NetSaleObligation,
    decimal NetCashHeld,
    decimal Outstanding,
    decimal RefundDueNow);
