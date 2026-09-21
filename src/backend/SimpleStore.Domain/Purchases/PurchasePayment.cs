namespace SimpleStore.Domain.Purchases;

public sealed class PurchasePayment
{
    private PurchasePayment()
    {
    }

    private PurchasePayment(
        Guid id,
        Guid storeId,
        Guid purchaseId,
        decimal amount,
        PaymentMethod method,
        DateTimeOffset paidAt,
        Guid performedByUserId)
    {
        if (amount <= 0)
        {
            throw new DomainRuleException("invalid-payment-amount", "Payment amount must be greater than zero.");
        }
        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainRuleException(
                "invalid-payment-precision",
                "Payment amount supports at most two decimal places.");
        }

        if (!Enum.IsDefined(method))
        {
            throw new DomainRuleException("invalid-payment-method", "Payment method is invalid.");
        }

        Id = id;
        StoreId = storeId;
        PurchaseId = purchaseId;
        Amount = amount;
        Method = method;
        PaidAt = paidAt;
        PerformedByUserId = performedByUserId;
    }

    public Guid Id { get; private set; }

    public Guid StoreId { get; private set; }

    public Guid PurchaseId { get; private set; }

    public decimal Amount { get; private set; }

    public PaymentMethod Method { get; private set; }

    public DateTimeOffset PaidAt { get; private set; }

    public Guid PerformedByUserId { get; private set; }

    internal static PurchasePayment Create(
        Guid storeId,
        Guid purchaseId,
        PurchasePaymentInput input,
        DateTimeOffset paidAt,
        Guid performedByUserId) =>
        new(
            Guid.NewGuid(),
            storeId,
            purchaseId,
            input.Amount,
            input.Method,
            paidAt,
            performedByUserId);
}

public sealed record PurchasePaymentInput(decimal Amount, PaymentMethod Method);
