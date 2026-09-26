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

    [Fact]
    public void StoragePrecisionGuardAcceptsExactSupportedQuantityAndUnitCostScales()
    {
        InventoryStoragePrecision.EnsureQuantity(
            1.001m,
            "invalid-quantity",
            "Quantity");
        InventoryStoragePrecision.EnsureQuantity(
            InventoryStoragePrecision.MaxQuantity,
            "invalid-quantity",
            "Quantity");
        InventoryStoragePrecision.EnsureUnitCost(
            12.3456m,
            "invalid-cost",
            "Unit cost");
        InventoryStoragePrecision.EnsureUnitCost(
            InventoryStoragePrecision.MaxUnitCost,
            "invalid-cost",
            "Unit cost");
    }

    [Theory]
    [InlineData(0.0004)]
    [InlineData(1.2345)]
    public void StoragePrecisionGuardRejectsQuantityThatWouldBeRoundedBySql(decimal value)
    {
        var error = Assert.Throws<DomainRuleException>(() =>
            InventoryStoragePrecision.EnsureQuantity(
                value,
                "invalid-quantity-precision",
                "Quantity"));

        Assert.Equal("invalid-quantity-precision", error.Code);
    }

    [Fact]
    public void StoragePrecisionGuardRejectsExcessUnitCostScaleAndMagnitude()
    {
        Assert.Equal(
            "invalid-quantity-precision",
            Assert.Throws<DomainRuleException>(() =>
                InventoryStoragePrecision.EnsureQuantity(
                    InventoryStoragePrecision.MaxQuantity + 0.001m,
                    "invalid-quantity-precision",
                    "Quantity")).Code);
        Assert.Equal(
            "invalid-cost-precision",
            Assert.Throws<DomainRuleException>(() =>
                InventoryStoragePrecision.EnsureUnitCost(
                    12.34567m,
                    "invalid-cost-precision",
                    "Unit cost")).Code);
        Assert.Equal(
            "invalid-cost-precision",
            Assert.Throws<DomainRuleException>(() =>
                InventoryStoragePrecision.EnsureUnitCost(
                    InventoryStoragePrecision.MaxUnitCost + 0.0001m,
                    "invalid-cost-precision",
                    "Unit cost")).Code);
    }

    [Fact]
    public void StocktakeRejectsNegativeCountBeforeCreatingAResult()
    {
        var error = Assert.Throws<DomainRuleException>(() => StocktakeResult.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            new byte[8],
            -1,
            null,
            null,
            null,
            10,
            10,
            10,
            true,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow));

        Assert.Equal("invalid-stocktake-counted-quantity", error.Code);
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
