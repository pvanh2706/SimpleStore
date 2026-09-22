using SimpleStore.Domain.Purchases;

namespace SimpleStore.Domain.Returns;

public sealed class ReturnRefundPayment
{
    private ReturnRefundPayment() { }

    private ReturnRefundPayment(
        Guid id,
        Guid storeId,
        Guid returnId,
        decimal amount,
        PaymentMethod method,
        DateTimeOffset occurredAt,
        Guid performedByUserId)
    {
        if (amount <= 0 || decimal.Round(amount, 2) != amount)
        {
            throw new DomainRuleException("invalid-refund-amount", "Refund amount must be positive and have at most two decimal places.");
        }

        Id = id;
        StoreId = storeId;
        ReturnId = returnId;
        Amount = amount;
        Method = method;
        OccurredAt = occurredAt;
        PerformedByUserId = performedByUserId;
    }

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid ReturnId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid PerformedByUserId { get; private set; }

    internal static ReturnRefundPayment Create(
        Guid storeId,
        Guid returnId,
        decimal amount,
        PaymentMethod method,
        DateTimeOffset occurredAt,
        Guid performedByUserId) =>
        new(Guid.NewGuid(), storeId, returnId, amount, method, occurredAt, performedByUserId);
}
