using SimpleStore.Domain;
using SimpleStore.Domain.Purchases;
using Xunit;

namespace SimpleStore.Domain.Tests.Purchases;

public sealed class PurchaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateDraftStoresSupplierAndLines()
    {
        var supplierId = Guid.NewGuid();
        var purchase = CreateDraft(
            supplierId,
            [new PurchaseLineInput(Guid.NewGuid(), 2, 10_000)]);

        Assert.Equal(PurchaseStatus.Draft, purchase.Status);
        Assert.Equal(supplierId, purchase.SupplierId);
        Assert.Single(purchase.Lines);
        Assert.Empty(purchase.Payments);
        Assert.Equal(20_000, purchase.TotalAmount);
    }

    [Fact]
    public void DuplicateProductLineIsRejected()
    {
        var productId = Guid.NewGuid();

        var exception = Assert.Throws<DomainRuleException>(() => CreateDraft(
            Guid.NewGuid(),
            [
                new PurchaseLineInput(productId, 1, 100),
                new PurchaseLineInput(productId, 2, 100)
            ]));

        Assert.Equal("duplicate-purchase-product", exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveQuantityIsRejected(decimal quantity)
    {
        var exception = Assert.Throws<DomainRuleException>(() => CreateDraft(
            Guid.NewGuid(),
            [new PurchaseLineInput(Guid.NewGuid(), quantity, 100)]));

        Assert.Equal("invalid-purchase-quantity", exception.Code);
    }

    [Fact]
    public void NegativeUnitPriceIsRejected()
    {
        var exception = Assert.Throws<DomainRuleException>(() => CreateDraft(
            Guid.NewGuid(),
            [new PurchaseLineInput(Guid.NewGuid(), 1, -0.01m)]));

        Assert.Equal("invalid-purchase-unit-price", exception.Code);
    }

    [Fact]
    public void LineAmountUsesAwayFromZeroAtMidpoint()
    {
        var purchase = CreateDraft(
            Guid.NewGuid(),
            [new PurchaseLineInput(Guid.NewGuid(), 1.005m, 1m)]);

        Assert.Equal(1.01m, Assert.Single(purchase.Lines).LineAmount);
    }

    [Fact]
    public void PurchaseTotalIsSumOfRoundedLineAmounts()
    {
        var purchase = CreateDraft(
            Guid.NewGuid(),
            [
                new PurchaseLineInput(Guid.NewGuid(), 1.005m, 1m),
                new PurchaseLineInput(Guid.NewGuid(), 2, 10m)
            ]);

        Assert.Equal(21.01m, purchase.TotalAmount);
    }

    [Fact]
    public void CompletedPurchaseIsImmutable()
    {
        var purchase = CreateDraft(
            Guid.NewGuid(),
            [new PurchaseLineInput(Guid.NewGuid(), 1, 100)]);
        purchase.Complete([], Guid.NewGuid(), Now);

        var exception = Assert.Throws<DomainRuleException>(() => purchase.ReplaceDraft(
            Guid.NewGuid(),
            [],
            Now.AddMinutes(1)));

        Assert.Equal("purchase-completed-immutable", exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositivePaymentIsRejected(decimal amount)
    {
        var purchase = CreateDraft(
            Guid.NewGuid(),
            [new PurchaseLineInput(Guid.NewGuid(), 1, 100)]);

        var exception = Assert.Throws<DomainRuleException>(() => purchase.Complete(
            [new PurchasePaymentInput(amount, PaymentMethod.Cash)],
            Guid.NewGuid(),
            Now));

        Assert.Equal("invalid-payment-amount", exception.Code);
    }

    [Fact]
    public void OverpaymentIsRejected()
    {
        var purchase = CreateDraft(
            Guid.NewGuid(),
            [new PurchaseLineInput(Guid.NewGuid(), 1, 100)]);

        var exception = Assert.Throws<DomainRuleException>(() => purchase.Complete(
            [new PurchasePaymentInput(100.01m, PaymentMethod.Cash)],
            Guid.NewGuid(),
            Now));

        Assert.Equal("purchase-overpayment", exception.Code);
    }

    [Fact]
    public void NoPaymentCreatesFullOutstandingAmount()
    {
        var purchase = CreateDraft(
            Guid.NewGuid(),
            [new PurchaseLineInput(Guid.NewGuid(), 2, 100)]);

        purchase.Complete([], Guid.NewGuid(), Now);

        Assert.Equal(0, purchase.PaidAmount);
        Assert.Equal(200, purchase.OutstandingAmount);
    }

    [Fact]
    public void MultiplePaymentsContributeToPaidAndOutstandingAmounts()
    {
        var purchase = CreateDraft(
            Guid.NewGuid(),
            [new PurchaseLineInput(Guid.NewGuid(), 2, 100)]);

        purchase.Complete(
            [
                new PurchasePaymentInput(30, PaymentMethod.Cash),
                new PurchasePaymentInput(50, PaymentMethod.Transfer)
            ],
            Guid.NewGuid(),
            Now);

        Assert.Equal(80, purchase.PaidAmount);
        Assert.Equal(120, purchase.OutstandingAmount);
        Assert.Equal(2, purchase.Payments.Count);
    }

    private static Purchase CreateDraft(
        Guid supplierId,
        IReadOnlyCollection<PurchaseLineInput> lines) =>
        Purchase.CreateDraft(
            Guid.NewGuid(),
            supplierId,
            Guid.NewGuid(),
            lines,
            Now);
}
