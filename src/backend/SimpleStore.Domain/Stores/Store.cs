namespace SimpleStore.Domain.Stores;

public sealed class Store
{
    private Store()
    {
    }

    private Store(Guid id, Guid ownerUserId, string name, DateTimeOffset createdAt)
    {
        Id = id;
        OwnerUserId = ownerUserId;
        Name = name;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid OwnerUserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static Store Create(Guid ownerUserId, string name, DateTimeOffset createdAt)
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

        return new Store(Guid.NewGuid(), ownerUserId, normalizedName, createdAt);
    }
}
