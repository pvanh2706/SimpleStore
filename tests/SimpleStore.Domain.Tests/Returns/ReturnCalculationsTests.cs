using SimpleStore.Domain;
using SimpleStore.Domain.Returns;
using Xunit;

namespace SimpleStore.Domain.Tests.Returns;

public sealed class ReturnCalculationsTests
{
    [Fact]
    public void FinalReturnClosesFinancialRoundingResidualExactly()
    {
        var first = ReturnCalculations.CalculateLine(3, 10, 3.3333m, 6, 2, 0, 0, 0, 0, 1, true);
        var second = ReturnCalculations.CalculateLine(
            3, 10, 3.3333m, 6, 2, 1, first.ReturnLineAmount, 1, first.RestockedInventoryValue, 2, true);

        Assert.Equal(3.33m, first.ReturnLineAmount);
        Assert.Equal(6.67m, second.ReturnLineAmount);
        Assert.Equal(6, first.RestockedInventoryValue + second.RestockedInventoryValue);
    }

    [Fact]
    public void NoRestockDoesNotTransferDiscardedInventoryValueToLaterRestock()
    {
        var amounts = ReturnCalculations.CalculateLine(3, 6, 2, 6, 2, 1, 2, 0, 0, 2, true);

        Assert.Equal(4, amounts.ReturnLineAmount);
        Assert.Equal(4, amounts.RestockedInventoryValue);
    }

    [Fact]
    public void QuantityBeyondCumulativeRemainingIsRejected()
    {
        var exception = Assert.Throws<DomainRuleException>(() => ReturnCalculations.CalculateLine(
            5, 50, 10, 25, 5, 4, 40, 2, 10, 2, true));

        Assert.Equal("return-quantity-exceeds-remaining", exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.0001)]
    public void InvalidReturnQuantityIsRejected(decimal quantity)
    {
        var exception = Assert.Throws<DomainRuleException>(() => ReturnCalculations.CalculateLine(
            1, 10, 10, 0, 0, 0, 0, 0, 0, quantity, true));

        Assert.Equal("invalid-return-quantity", exception.Code);
    }

    [Fact]
    public void ZeroHistoricalCostRestocksZeroValue()
    {
        var result = ReturnCalculations.CalculateLine(1, 10, 10, 0, 0, 0, 0, 0, 0, 1, true);

        Assert.Equal(0, result.RestockedInventoryValue);
    }

    [Theory]
    [InlineData(100, 40, 0, 0, 30, 0, 30)]
    [InlineData(100, 100, 0, 0, 30, 30, 0)]
    [InlineData(100, 70, 20, 10, 30, 10, 0)]
    public void RefundReducesObligationBeforePayingCash(
        decimal total,
        decimal collected,
        decimal previousReturns,
        decimal previousRefunds,
        decimal currentReturn,
        decimal expectedRefund,
        decimal expectedOutstanding)
    {
        var result = ReturnCalculations.CalculateFinancials(
            total, collected, previousReturns, previousRefunds, currentReturn);

        Assert.Equal(expectedRefund, result.RefundDueNow);
        Assert.Equal(expectedOutstanding, result.Outstanding);
    }
}
