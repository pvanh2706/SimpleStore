using SimpleStore.Domain.Purchases;

namespace SimpleStore.Domain.Returns;

public sealed class CustomerReturn
{
    private readonly List<ReturnLine> _lines = [];
    private readonly List<ReturnRefundPayment> _refundPayments = [];

    private CustomerReturn() { }

    private CustomerReturn(Guid id, Guid storeId, Guid originalSaleId, Guid completedByUserId, DateTimeOffset completedAt)
    {
        Id = id;
        StoreId = storeId;
        OriginalSaleId = originalSaleId;
        CompletedByUserId = completedByUserId;
        Status = ReturnStatus.Completed;
        CreatedAt = completedAt;
        CompletedAt = completedAt;
    }

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid OriginalSaleId { get; private set; }
    public ReturnStatus Status { get; private set; }
    public decimal TotalReturnAmount { get; private set; }
    public decimal RefundAmount { get; private set; }
    public Guid CompletedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset CompletedAt { get; private set; }
    public IReadOnlyCollection<ReturnLine> Lines => _lines;
    public IReadOnlyCollection<ReturnRefundPayment> RefundPayments => _refundPayments;

    public static CustomerReturn Complete(
        Guid storeId,
        Guid originalSaleId,
        Guid completedByUserId,
        IReadOnlyCollection<ReturnLineInput> lines,
        decimal refundAmount,
        PaymentMethod? refundMethod,
        DateTimeOffset completedAt)
    {
        if (storeId == Guid.Empty || originalSaleId == Guid.Empty || completedByUserId == Guid.Empty)
        {
            throw new DomainRuleException("return-context-required", "Return store, original sale and actor are required.");
        }

        if (lines.Count == 0)
        {
            throw new DomainRuleException("return-lines-required", "A return requires at least one line.");
        }

        if (lines.GroupBy(line => line.OriginalSaleLineId).Any(group => group.Count() > 1))
        {
            throw new DomainRuleException("duplicate-return-line", "An original sale line can appear only once in a return.");
        }

        var result = new CustomerReturn(Guid.NewGuid(), storeId, originalSaleId, completedByUserId, completedAt);
        result._lines.AddRange(lines.Select(line => ReturnLine.Create(storeId, result.Id, line)));
        result.TotalReturnAmount = result._lines.Sum(line => line.ReturnLineAmount);

        if (refundAmount < 0 || refundAmount > result.TotalReturnAmount)
        {
            throw new DomainRuleException("invalid-refund-amount", "Refund amount is outside the current return value.");
        }

        if (refundAmount > 0 && refundMethod is null)
        {
            throw new DomainRuleException("refund-method-required", "A refund method is required when money is refunded.");
        }

        if (refundAmount == 0 && refundMethod is not null)
        {
            throw new DomainRuleException("refund-method-not-applicable", "Refund method is not applicable when no refund is due.");
        }

        result.RefundAmount = refundAmount;
        if (refundAmount > 0)
        {
            result._refundPayments.Add(ReturnRefundPayment.Create(
                storeId,
                result.Id,
                refundAmount,
                refundMethod!.Value,
                completedAt,
                completedByUserId));
        }

        return result;
    }
}
