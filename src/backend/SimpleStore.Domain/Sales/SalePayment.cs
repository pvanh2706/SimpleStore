using SimpleStore.Domain.Purchases;

namespace SimpleStore.Domain.Sales;

public sealed class SalePayment
{
    private SalePayment()
    {
    }

    private SalePayment(
        Guid id,
        Guid storeId,
        Guid saleId,
        SalePaymentInput input,
        DateTimeOffset paidAt,
        Guid performedByUserId)
    {
        if (input.Amount <= 0 || decimal.Round(input.Amount, 2) != input.Amount)
        {
            throw new DomainRuleException(
                "invalid-payment-amount",
                "Payment amount must be greater than zero and support at most two decimal places.");
        }

        if (!Enum.IsDefined(input.Method))
        {
            throw new DomainRuleException("invalid-payment-method", "Payment method is invalid.");
        }

        Id = id;
        StoreId = storeId;
        SaleId = saleId;
        Amount = input.Amount;
        Method = input.Method;
        PaidAt = paidAt;
        PerformedByUserId = performedByUserId;
    }

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid SaleId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public DateTimeOffset PaidAt { get; private set; }
    public Guid PerformedByUserId { get; private set; }

    internal static SalePayment Create(
        Guid storeId,
        Guid saleId,
        SalePaymentInput input,
        DateTimeOffset paidAt,
        Guid performedByUserId) =>
        new(Guid.NewGuid(), storeId, saleId, input, paidAt, performedByUserId);
}

public sealed record SalePaymentInput(decimal Amount, PaymentMethod Method);
