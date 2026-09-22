namespace SimpleStore.Domain.Corrections;

public sealed class PurchaseVoid
{
    private PurchaseVoid() { }

    private PurchaseVoid(Guid id, Guid storeId, Guid purchaseId, string reason, Guid userId, DateTimeOffset now)
    {
        Id = id;
        StoreId = storeId;
        OriginalPurchaseId = purchaseId;
        Reason = SaleVoid.NormalizeReason(reason);
        VoidedByUserId = userId;
        VoidedAt = now;
        CreatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid OriginalPurchaseId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid VoidedByUserId { get; private set; }
    public DateTimeOffset VoidedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static PurchaseVoid Create(Guid storeId, Guid purchaseId, string reason, Guid userId, DateTimeOffset now) =>
        new(Guid.NewGuid(), storeId, purchaseId, reason, userId, now);
}
