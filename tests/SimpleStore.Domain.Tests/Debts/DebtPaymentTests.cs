using SimpleStore.Domain;
using SimpleStore.Domain.Debts;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Stores;
using Xunit;

namespace SimpleStore.Domain.Tests.Debts;

public sealed class DebtPaymentTests
{
    [Fact]
    public void CustomerCollectionHasCanonicalDirectionPartyAndTrimmedNote()
    {
        var customerId = Guid.NewGuid();
        var payment = DebtPayment.RecordCustomerCollection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            customerId,
            12.34m,
            PaymentMethod.Cash,
            "  morning collection  ",
            DateTimeOffset.UtcNow,
            Guid.NewGuid());

        Assert.Equal(DebtPaymentDirection.MoneyIn, payment.Direction);
        Assert.Equal(DebtPaymentPurpose.CustomerDebtCollection, payment.Purpose);
        Assert.Equal(customerId, payment.CustomerId);
        Assert.Null(payment.SupplierId);
        Assert.Equal("morning collection", payment.Note);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyNotesAreCanonicalNull(string? note)
    {
        var payment = DebtPayment.RecordSupplierSettlement(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, PaymentMethod.Transfer,
            note, DateTimeOffset.UtcNow, Guid.NewGuid());

        Assert.Null(payment.Note);
    }

    [Fact]
    public void NoteBoundaryAndPrecisionAreEnforced()
    {
        var valid = DebtPayment.RecordCustomerCollection(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1.23m, PaymentMethod.Cash,
            new string('a', 250), DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.Equal(250, valid.Note!.Length);

        var note = Assert.Throws<DomainRuleException>(() => DebtPayment.RecordCustomerCollection(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, PaymentMethod.Cash,
            new string('a', 251), DateTimeOffset.UtcNow, Guid.NewGuid()));
        Assert.Equal("invalid-note-length", note.Code);

        var precision = Assert.Throws<DomainRuleException>(() => DebtPayment.RecordCustomerCollection(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1.001m, PaymentMethod.Cash,
            null, DateTimeOffset.UtcNow, Guid.NewGuid()));
        Assert.Equal("invalid-payment-precision", precision.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveAmountsAreRejected(decimal amount)
    {
        var exception = Assert.Throws<DomainRuleException>(() => DebtPayment.RecordSupplierSettlement(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), amount, PaymentMethod.Cash,
            null, DateTimeOffset.UtcNow, Guid.NewGuid()));
        Assert.Equal("invalid-payment-amount", exception.Code);
    }

    [Fact]
    public void UnknownPaymentMethodIsRejected()
    {
        var exception = Assert.Throws<DomainRuleException>(() => DebtPayment.RecordCustomerCollection(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, (PaymentMethod)999,
            null, DateTimeOffset.UtcNow, Guid.NewGuid()));
        Assert.Equal("invalid-payment-method", exception.Code);
    }

    [Theory]
    [InlineData(30, 50, 30, 20, 0)]
    [InlineData(50, 50, 50, 0, 0)]
    [InlineData(100, 50, 50, 0, 50)]
    [InlineData(0, 50, 0, 50, 0)]
    public void AggregateReturnUsesDebtBeforeActualRefund(
        decimal debt,
        decimal returnValue,
        decimal expectedDebtReduction,
        decimal expectedRefund,
        decimal expectedEndingDebt)
    {
        var result = DebtCalculations.CalculateAggregateReturn(returnValue, debt);

        Assert.Equal(expectedDebtReduction, result.DebtReduction);
        Assert.Equal(expectedRefund, result.RequiredActualRefund);
        Assert.Equal(expectedEndingDebt, result.EndingCustomerDebt);
    }

    [Fact]
    public void CustomerAndSupplierHistoryFormulasDoNotClampCorruptNegativeState()
    {
        Assert.Equal(-10, DebtCalculations.CustomerOutstanding(100, 50, 20, 0, 40));
        Assert.Equal(-10, DebtCalculations.SupplierOutstanding(100, 50, 60));
    }

    [Fact]
    public void StoreDefaultsToCanonicalIanaTimezone()
    {
        var store = Store.Create(Guid.NewGuid(), "Store", DateTimeOffset.UtcNow);
        Assert.Equal("Asia/Ho_Chi_Minh", store.TimeZoneId);

        var exception = Assert.Throws<DomainRuleException>(() =>
            Store.Create(Guid.NewGuid(), "Store", DateTimeOffset.UtcNow, "SE Asia Standard Time"));
        Assert.Equal("invalid-store-timezone", exception.Code);
    }
}
