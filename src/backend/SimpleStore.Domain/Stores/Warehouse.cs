namespace SimpleStore.Domain.Stores;

public sealed class Warehouse
{
    private Warehouse()
    {
    }

    private Warehouse(
        Guid id,
        Guid storeId,
        string name,
        bool isMain,
        DateTimeOffset createdAt)
    {
        Id = id;
        StoreId = storeId;
        Name = name;
        IsMain = isMain;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid StoreId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public bool IsMain { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Warehouse CreateMain(Guid storeId, DateTimeOffset createdAt)
    {
        if (storeId == Guid.Empty)
        {
            throw new DomainRuleException("store-required", "Warehouse store is required.");
        }

        return new Warehouse(Guid.NewGuid(), storeId, "Kho chính", true, createdAt);
    }
}
