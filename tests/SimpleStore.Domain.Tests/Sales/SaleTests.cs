using SimpleStore.Domain;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Sales;
using Xunit;

namespace SimpleStore.Domain.Tests.Sales;

public sealed class SaleTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CompletionCalculatesRoundedLinesTotalAndMultiplePayments()
    {
        var sale = Complete(
            [Line(Guid.NewGuid(), 1.005m, 1m), Line(Guid.NewGuid(), 2, 10m)],
            [new SalePaymentInput(10, PaymentMethod.Cash), new SalePaymentInput(5, PaymentMethod.Transfer)],
            Guid.NewGuid());

        Assert.Equal(SaleStatus.Completed, sale.Status);
        Assert.Equal(21.01m, sale.TotalAmount);
        Assert.Equal(15, sale.PaidAmount);
        Assert.Equal(6.01m, sale.OutstandingAmount);
        Assert.Equal(1.01m, sale.Lines.First().LineAmount);
    }

    [Fact]
    public void DuplicateProductIsRejected()
    {
        var productId = Guid.NewGuid();
        var exception = Assert.Throws<DomainRuleException>(() => Complete(
            [Line(productId, 1, 10), Line(productId, 2, 10)],
            [new SalePaymentInput(30, PaymentMethod.Cash)],
            null));

        Assert.Equal("duplicate-sale-product", exception.Code);
    }

    [Fact]
    public void CreditSaleRequiresCustomerButFullyPaidSaleDoesNot()
    {
        var exception = Assert.Throws<DomainRuleException>(() => Complete(
            [Line(Guid.NewGuid(), 1, 100)],
            [],
            null));
        Assert.Equal("customer-required-for-credit", exception.Code);

        var fullyPaid = Complete(
            [Line(Guid.NewGuid(), 1, 100)],
            [new SalePaymentInput(100, PaymentMethod.Cash)],
            null);
        Assert.Null(fullyPaid.CustomerId);
        Assert.Equal(0, fullyPaid.OutstandingAmount);
    }

    [Fact]
    public void OverpaymentAndInvalidPaymentAreRejected()
    {
        var overpayment = Assert.Throws<DomainRuleException>(() => Complete(
            [Line(Guid.NewGuid(), 1, 100)],
            [new SalePaymentInput(100.01m, PaymentMethod.Cash)],
            null));
        Assert.Equal("sale-overpayment", overpayment.Code);

        var invalid = Assert.Throws<DomainRuleException>(() => Complete(
            [Line(Guid.NewGuid(), 1, 100)],
            [new SalePaymentInput(0, PaymentMethod.Cash)],
            Guid.NewGuid()));
        Assert.Equal("invalid-payment-amount", invalid.Code);
    }

    public static TheoryData<bool, decimal, decimal?, decimal, CostReliability> CostCases => new()
    {
        { true, 0m, null, 0m, CostReliability.Reliable },
        { false, 0m, 0m, 0m, CostReliability.Estimated },
        { false, 0m, 12.5m, 12.5m, CostReliability.Estimated },
        { false, 0m, null, 0m, CostReliability.Unavailable }
    };

    [Theory]
    [MemberData(nameof(CostCases))]
    public void CostResolutionUsesExplicitKnownState(
        bool hasAverageCost,
        decimal averageCost,
        decimal? referenceCost,
        decimal expectedCost,
        CostReliability expectedReliability)
    {
        var balance = InventoryBalance.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            hasAverageCost ? OpeningInventory.Create(1, averageCost) : OpeningInventory.Create(0, null),
            Now);

        var result = balance.ResolveSaleCost(referenceCost);

        Assert.Equal(expectedCost, result.UnitCost);
        Assert.Equal(expectedReliability, result.Reliability);
    }

    private static Sale Complete(
        IReadOnlyCollection<SaleLineInput> lines,
        IReadOnlyCollection<SalePaymentInput> payments,
        Guid? customerId) =>
        Sale.Complete(
            Guid.NewGuid(),
            Guid.NewGuid(),
            customerId,
            Guid.NewGuid(),
            lines,
            payments,
            Now);

    private static SaleLineInput Line(Guid productId, decimal quantity, decimal price) =>
        new(productId, "Product", "SKU", "unit", quantity, price, 5, CostReliability.Reliable);
}
