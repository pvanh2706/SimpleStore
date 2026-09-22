namespace SimpleStore.Domain.Corrections;

public sealed class SaleVoid
{
    public const int MaxReasonLength = 500;
    private SaleVoid() { }

    private SaleVoid(Guid id, Guid storeId, Guid originalSaleId, string reason, Guid userId, DateTimeOffset now)
    {
        Id = id;
        StoreId = storeId;
        OriginalSaleId = originalSaleId;
        Reason = NormalizeReason(reason);
        VoidedByUserId = userId;
        VoidedAt = now;
        CreatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid OriginalSaleId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public Guid VoidedByUserId { get; private set; }
    public DateTimeOffset VoidedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static SaleVoid Create(Guid storeId, Guid saleId, string reason, Guid userId, DateTimeOffset now) =>
        new(Guid.NewGuid(), storeId, saleId, reason, userId, now);

    public static string NormalizeReason(string? reason)
    {
        var value = reason?.Trim() ?? string.Empty;
        if (value.Length == 0) throw new DomainRuleException("void-reason-required", "Void reason is required.");
        if (value.Length > MaxReasonLength) throw new DomainRuleException("void-reason-too-long", $"Void reason cannot exceed {MaxReasonLength} characters.");
        return value;
    }
}
