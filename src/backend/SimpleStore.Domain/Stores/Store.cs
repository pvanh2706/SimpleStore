namespace SimpleStore.Domain.Stores;

public sealed class Store
{
    public const int MaxTimeZoneIdLength = 128;
    public const string DefaultTimeZoneId = "Asia/Ho_Chi_Minh";

    private Store()
    {
    }

    private Store(Guid id, Guid ownerUserId, string name, string timeZoneId, DateTimeOffset createdAt)
    {
        Id = id;
        OwnerUserId = ownerUserId;
        Name = name;
        TimeZoneId = NormalizeTimeZoneId(timeZoneId);
        AllowNegativeStock = false;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid OwnerUserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool AllowNegativeStock { get; private set; }

    public string TimeZoneId { get; private set; } = DefaultTimeZoneId;

    public DateTimeOffset CreatedAt { get; private set; }

    public static Store Create(
        Guid ownerUserId,
        string name,
        DateTimeOffset createdAt,
        string timeZoneId = DefaultTimeZoneId)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new DomainRuleException("owner-required", "Store owner is required.");
        }

        var normalizedName = name?.Trim() ?? string.Empty;
        if (normalizedName.Length is < 1 or > 120)
        {
            throw new DomainRuleException(
                "invalid-store-name",
                "Store name is required and must not exceed 120 characters.");
        }

        return new Store(Guid.NewGuid(), ownerUserId, normalizedName, timeZoneId, createdAt);
    }

    public NegativeStockSettingAudit ChangeNegativeStockPolicy(
        bool allowNegativeStock,
        Guid changedByUserId,
        DateTimeOffset changedAt)
    {
        if (changedByUserId == Guid.Empty)
        {
            throw new DomainRuleException("user-required", "The user changing the setting is required.");
        }

        var audit = NegativeStockSettingAudit.Create(
            Id,
            AllowNegativeStock,
            allowNegativeStock,
            changedByUserId,
            changedAt);
        AllowNegativeStock = allowNegativeStock;
        return audit;
    }

    public void ChangeTimeZone(string timeZoneId) =>
        TimeZoneId = NormalizeTimeZoneId(timeZoneId);

    private static string NormalizeTimeZoneId(string? timeZoneId)
    {
        var normalized = timeZoneId?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > MaxTimeZoneIdLength
            || !TimeZoneInfo.TryFindSystemTimeZoneById(normalized, out var timeZone)
            || !timeZone.HasIanaId)
        {
            throw new DomainRuleException(
                "invalid-store-timezone",
                "Store timezone must be a valid canonical IANA timezone ID.");
        }

        return timeZone.Id;
    }
}
