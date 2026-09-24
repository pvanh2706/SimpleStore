using SimpleStore.Domain;
using SimpleStore.Domain.Experiments;
using Xunit;

namespace SimpleStore.Domain.Tests.Experiments;

public sealed class C14ExperimentEventTests
{
    [Fact]
    public void TodayOpenedRequiresNoProductTarget()
    {
        var result = C14ExperimentEvent.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            C14ExperimentEventTypes.TodayOpened,
            null,
            null,
            new DateOnly(2026, 9, 23),
            DateTimeOffset.UtcNow);

        Assert.Null(result.ProductId);
        Assert.Null(result.AttentionKind);
        Assert.Throws<DomainRuleException>(() => C14ExperimentEvent.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            C14ExperimentEventTypes.TodayOpened, Guid.NewGuid(),
            C14AttentionKinds.OutOfStock, new DateOnly(2026, 9, 23), DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(C14ExperimentEventTypes.SignalShown)]
    [InlineData(C14ExperimentEventTypes.WhyOpened)]
    [InlineData(C14ExperimentEventTypes.PurchaseDraftStarted)]
    public void ProductEventsRequireProductAndClosedAttentionKind(string eventType)
    {
        var productId = Guid.NewGuid();
        var result = C14ExperimentEvent.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), eventType,
            productId, C14AttentionKinds.LowStockRisk,
            new DateOnly(2026, 9, 23), DateTimeOffset.UtcNow);

        Assert.Equal(productId, result.ProductId);
        Assert.Throws<DomainRuleException>(() => C14ExperimentEvent.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), eventType,
            null, C14AttentionKinds.LowStockRisk,
            new DateOnly(2026, 9, 23), DateTimeOffset.UtcNow));
        Assert.Throws<DomainRuleException>(() => C14ExperimentEvent.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), eventType,
            productId, "ArbitraryRule",
            new DateOnly(2026, 9, 23), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RejectsEventTypesOutsideTheFourApprovedValues()
    {
        var exception = Assert.Throws<DomainRuleException>(() => C14ExperimentEvent.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "GenericClick",
            null, null, new DateOnly(2026, 9, 23), DateTimeOffset.UtcNow));

        Assert.Equal("invalid-c14-event-type", exception.Code);
    }
}
