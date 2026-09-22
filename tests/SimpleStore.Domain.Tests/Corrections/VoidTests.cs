using SimpleStore.Domain;
using SimpleStore.Domain.Corrections;
using Xunit;

namespace SimpleStore.Domain.Tests.Corrections;

public sealed class VoidTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void VoidReasonIsRequired(string reason)
    {
        var exception = Assert.Throws<DomainRuleException>(() => SaleVoid.Create(
            Guid.NewGuid(), Guid.NewGuid(), reason, Guid.NewGuid(), DateTimeOffset.UtcNow));

        Assert.Equal("void-reason-required", exception.Code);
    }

    [Fact]
    public void VoidReasonIsTrimmedAndLimited()
    {
        var item = PurchaseVoid.Create(
            Guid.NewGuid(), Guid.NewGuid(), "  correction  ", Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal("correction", item.Reason);

        var exception = Assert.Throws<DomainRuleException>(() => SaleVoid.Create(
            Guid.NewGuid(), Guid.NewGuid(), new string('x', SaleVoid.MaxReasonLength + 1),
            Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Equal("void-reason-too-long", exception.Code);
    }
}
