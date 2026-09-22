using SimpleStore.Domain;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Returns;
using Xunit;

namespace SimpleStore.Domain.Tests.Returns;

public sealed class CustomerReturnTests
{
    [Fact]
    public void ReturnRequiresAtLeastOneLine()
    {
        var exception = Assert.Throws<DomainRuleException>(() => CustomerReturn.Complete(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), [], 0, null, DateTimeOffset.UtcNow));

        Assert.Equal("return-lines-required", exception.Code);
    }

    [Fact]
    public void CompleteCreatesImmutableHistoricalLineAndActualRefund()
    {
        var item = CustomerReturn.Complete(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new ReturnLineInput(Guid.NewGuid(), Guid.NewGuid(), 2, true, 10, 20, 6, 12)],
            5,
            PaymentMethod.Cash,
            DateTimeOffset.UtcNow);

        Assert.Equal(ReturnStatus.Completed, item.Status);
        Assert.Equal(20, item.TotalReturnAmount);
        Assert.Equal(5, item.RefundAmount);
        Assert.Single(item.Lines);
        Assert.Single(item.RefundPayments);
    }

    [Fact]
    public void DuplicateOriginalLineIsRejected()
    {
        var lineId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var exception = Assert.Throws<DomainRuleException>(() => CustomerReturn.Complete(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            [
                new ReturnLineInput(lineId, productId, 1, true, 10, 10, 5, 5),
                new ReturnLineInput(lineId, productId, 1, false, 10, 10, 5, 0)
            ],
            0, null, DateTimeOffset.UtcNow));

        Assert.Equal("duplicate-return-line", exception.Code);
    }

    [Fact]
    public void RefundMethodMustMatchActualMoneyOut()
    {
        var input = new[] { new ReturnLineInput(Guid.NewGuid(), Guid.NewGuid(), 1, false, 10, 10, 5, 0) };

        Assert.Equal("refund-method-required", Assert.Throws<DomainRuleException>(() => CustomerReturn.Complete(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), input, 1, null, DateTimeOffset.UtcNow)).Code);
        Assert.Equal("refund-method-not-applicable", Assert.Throws<DomainRuleException>(() => CustomerReturn.Complete(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), input, 0, PaymentMethod.Cash, DateTimeOffset.UtcNow)).Code);
    }

    [Fact]
    public void CompletedReturnHasNoPublicMutationSurface()
    {
        Assert.DoesNotContain(typeof(CustomerReturn).GetProperties(), property => property.SetMethod?.IsPublic == true);
        Assert.DoesNotContain(typeof(CustomerReturn).GetMethods(), method =>
            method.IsPublic && method.Name is "Update" or "Delete" or "AddLine");
    }
}
