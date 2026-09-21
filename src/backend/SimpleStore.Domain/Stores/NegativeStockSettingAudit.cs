namespace SimpleStore.Domain.Stores;

public sealed class NegativeStockSettingAudit
{
    private NegativeStockSettingAudit()
    {
    }

    private NegativeStockSettingAudit(
        Guid id,
        Guid storeId,
        bool oldValue,
        bool newValue,
        Guid changedByUserId,
        DateTimeOffset changedAt)
    {
        Id = id;
        StoreId = storeId;
        OldValue = oldValue;
        NewValue = newValue;
        ChangedByUserId = changedByUserId;
        ChangedAt = changedAt;
    }

    public Guid Id { get; private set; }

    public Guid StoreId { get; private set; }

    public bool OldValue { get; private set; }

    public bool NewValue { get; private set; }

    public Guid ChangedByUserId { get; private set; }

    public DateTimeOffset ChangedAt { get; private set; }

    internal static NegativeStockSettingAudit Create(
        Guid storeId,
        bool oldValue,
        bool newValue,
        Guid changedByUserId,
        DateTimeOffset changedAt) =>
        new(Guid.NewGuid(), storeId, oldValue, newValue, changedByUserId, changedAt);
}
