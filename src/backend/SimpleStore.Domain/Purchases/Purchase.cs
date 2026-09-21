namespace SimpleStore.Domain.Purchases;

public sealed class Purchase
{
    private readonly List<PurchaseLine> _lines = [];
    private readonly List<PurchasePayment> _payments = [];

    private Purchase()
    {
    }

    private Purchase(
        Guid id,
        Guid storeId,
        Guid supplierId,
        Guid createdByUserId,
        DateTimeOffset createdAt)
    {
        Id = id;
        StoreId = storeId;
        CreatedByUserId = createdByUserId;
        Status = PurchaseStatus.Draft;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        SetSupplier(supplierId);
    }

    public Guid Id { get; private set; }

    public Guid StoreId { get; private set; }

    public Guid SupplierId { get; private set; }

    public PurchaseStatus Status { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public decimal TotalAmount { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<PurchaseLine> Lines => _lines;

    public IReadOnlyCollection<PurchasePayment> Payments => _payments;

    public decimal PaidAmount => _payments.Sum(payment => payment.Amount);

    public decimal OutstandingAmount => TotalAmount - PaidAmount;

    public static Purchase CreateDraft(
        Guid storeId,
        Guid supplierId,
        Guid createdByUserId,
        IReadOnlyCollection<PurchaseLineInput> lines,
        DateTimeOffset createdAt)
    {
        if (storeId == Guid.Empty)
        {
            throw new DomainRuleException("store-required", "Purchase store is required.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainRuleException("user-required", "Purchase creator is required.");
        }

        var purchase = new Purchase(Guid.NewGuid(), storeId, supplierId, createdByUserId, createdAt);
        purchase.ReplaceDraft(supplierId, lines, createdAt);
        return purchase;
    }

    public void ReplaceDraft(
        Guid supplierId,
        IReadOnlyCollection<PurchaseLineInput> lines,
        DateTimeOffset updatedAt)
    {
        EnsureDraft();
        EnsureSupplier(supplierId);

        var duplicateProduct = lines
            .GroupBy(line => line.ProductId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateProduct is not null)
        {
            throw new DomainRuleException(
                "duplicate-purchase-product",
                "A product can appear only once in a purchase.");
        }

        var replacementLines = lines
            .Select(line => PurchaseLine.Create(
                StoreId,
                Id,
                line.ProductId,
                line.Quantity,
                line.UnitPrice))
            .ToArray();

        SupplierId = supplierId;
        _lines.Clear();
        _lines.AddRange(replacementLines);
        TotalAmount = _lines.Sum(line => line.LineAmount);
        UpdatedAt = updatedAt;
    }

    public void Complete(
        IReadOnlyCollection<PurchasePaymentInput> payments,
        Guid performedByUserId,
        DateTimeOffset completedAt)
    {
        EnsureDraft();
        if (_lines.Count == 0)
        {
            throw new DomainRuleException("purchase-lines-required", "A purchase requires at least one line.");
        }

        var total = _lines.Sum(line => line.LineAmount);
        var completedPayments = payments
            .Select(payment => PurchasePayment.Create(
                StoreId,
                Id,
                payment,
                completedAt,
                performedByUserId))
            .ToArray();
        var paid = completedPayments.Sum(payment => payment.Amount);
        if (paid > total)
        {
            throw new DomainRuleException("purchase-overpayment", "Payments cannot exceed the purchase total.");
        }

        _payments.Clear();
        _payments.AddRange(completedPayments);

        TotalAmount = total;
        Status = PurchaseStatus.Completed;
        CompletedAt = completedAt;
        UpdatedAt = completedAt;
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseStatus.Draft)
        {
            throw new DomainRuleException(
                "purchase-completed-immutable",
                "A completed purchase cannot be changed.");
        }
    }

    private void SetSupplier(Guid supplierId)
    {
        EnsureSupplier(supplierId);
        SupplierId = supplierId;
    }

    private static void EnsureSupplier(Guid supplierId)
    {
        if (supplierId == Guid.Empty)
        {
            throw new DomainRuleException("supplier-required", "A supplier is required.");
        }

    }
}
