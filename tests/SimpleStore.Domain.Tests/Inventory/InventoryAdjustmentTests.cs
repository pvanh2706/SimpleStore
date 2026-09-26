using SimpleStore.Domain.Inventory;
using Xunit;

namespace SimpleStore.Domain.Tests.Inventory;

public sealed class InventoryAdjustmentTests
{
    [Fact]
    public void PositiveAdjustmentUsesReliableAverageAndPreservesIt()
    {
        var balance = CreateBalance(10, 20_000);

        var cost = InventoryAdjustmentCostResolver.Resolve(balance, 2, null, 99_000);
        balance.ApplyInventoryAdjustment(
            2,
            cost.InventoryValueDelta,
            cost.UnitCost,
            cost.EstablishesReliableBasis,
            DateTimeOffset.UtcNow);

        Assert.Equal(20_000, cost.UnitCost);
        Assert.Equal(40_000, cost.InventoryValueDelta);
        Assert.Equal(CostReliability.Reliable, cost.Reliability);
        Assert.Equal(12, balance.QuantityOnHand);
        Assert.Equal(240_000, balance.InventoryValue);
        Assert.Equal(20_000, balance.AverageCost);
        Assert.True(balance.HasAverageCost);
    }

    [Fact]
    public void PositiveAdjustmentWithoutReliableCostRequiresExplicitCostAndIgnoresReferenceCost()
    {
        var balance = CreateUnreliableNonZeroBalance();

        var error = Assert.Throws<DomainRuleException>(() =>
            InventoryAdjustmentCostResolver.Resolve(balance, 2, null, 15_000));

        Assert.Equal("adjustment-unit-cost-required", error.Code);
    }

    [Fact]
    public void ExplicitCostOnNonCleanUnreliableBalanceDoesNotClaimReliableAverage()
    {
        var balance = CreateUnreliableNonZeroBalance();
        var cost = InventoryAdjustmentCostResolver.Resolve(balance, 2, 15_000, 99_000);

        balance.ApplyInventoryAdjustment(
            2,
            cost.InventoryValueDelta,
            cost.UnitCost,
            cost.EstablishesReliableBasis,
            DateTimeOffset.UtcNow);

        Assert.False(cost.EstablishesReliableBasis);
        Assert.False(balance.HasAverageCost);
    }

    [Fact]
    public void ExplicitCostOnCleanZeroBalanceEstablishesReliableCostBasis()
    {
        var balance = CreateBalance(0, null);
        var cost = InventoryAdjustmentCostResolver.Resolve(balance, 3, 12_500, 99_000);

        balance.ApplyInventoryAdjustment(
            3,
            cost.InventoryValueDelta,
            cost.UnitCost,
            cost.EstablishesReliableBasis,
            DateTimeOffset.UtcNow);

        Assert.True(cost.EstablishesReliableBasis);
        Assert.Equal(3, balance.QuantityOnHand);
        Assert.Equal(37_500, balance.InventoryValue);
        Assert.Equal(12_500, balance.AverageCost);
        Assert.True(balance.HasAverageCost);
    }

    [Theory]
    [InlineData(true, true, 20_000, "Reliable")]
    [InlineData(false, true, 14_000, "Estimated")]
    [InlineData(false, false, 0, "Unavailable")]
    public void NegativeAdjustmentUsesTheApprovedOutboundCostFallback(
        bool reliable,
        bool hasReferenceCost,
        decimal expectedUnitCost,
        string expectedReliability)
    {
        var balance = reliable ? CreateBalance(10, 20_000) : CreateBalance(0, null);

        var cost = InventoryAdjustmentCostResolver.Resolve(
            balance,
            -2,
            null,
            hasReferenceCost ? 14_000 : null);

        Assert.Equal(expectedUnitCost, cost.UnitCost);
        Assert.Equal(-2 * expectedUnitCost, cost.InventoryValueDelta);
        Assert.Equal(Enum.Parse<CostReliability>(expectedReliability), cost.Reliability);
        Assert.False(cost.EstablishesReliableBasis);
    }

    [Fact]
    public void AdjustmentRejectsZeroQuantityAndExplicitCostForReliableOrOutboundChanges()
    {
        var reliable = CreateBalance(10, 20_000);

        Assert.Equal(
            "adjustment-quantity-required",
            Assert.Throws<DomainRuleException>(() =>
                InventoryAdjustmentCostResolver.Resolve(reliable, 0, null, null)).Code);
        Assert.Equal(
            "adjustment-unit-cost-not-allowed",
            Assert.Throws<DomainRuleException>(() =>
                InventoryAdjustmentCostResolver.Resolve(reliable, 1, 1, null)).Code);
        Assert.Equal(
            "adjustment-unit-cost-not-allowed",
            Assert.Throws<DomainRuleException>(() =>
                InventoryAdjustmentCostResolver.Resolve(reliable, -1, 1, null)).Code);
    }

    private static InventoryBalance CreateBalance(decimal quantity, decimal? cost) =>
        InventoryBalance.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            OpeningInventory.Create(quantity, cost),
            DateTimeOffset.UtcNow);

    private static InventoryBalance CreateUnreliableNonZeroBalance()
    {
        var balance = CreateBalance(10, 10);
        balance.IssueSale(20, 200, DateTimeOffset.UtcNow);
        balance.ReceivePurchase(11, 11, DateTimeOffset.UtcNow);
        return balance;
    }
}
