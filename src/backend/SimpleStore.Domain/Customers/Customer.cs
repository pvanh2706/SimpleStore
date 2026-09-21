namespace SimpleStore.Domain.Customers;

public sealed class Customer
{
    public const int MaxNameLength = 160;
    public const int MaxPhoneLength = 32;

    private Customer()
    {
    }

    private Customer(Guid id, Guid storeId, string name, string? phone, DateTimeOffset createdAt)
    {
        Id = id;
        StoreId = storeId;
        Name = NormalizeRequired(name);
        Phone = NormalizePhone(phone);
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid StoreId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Customer Create(Guid storeId, string name, string? phone, DateTimeOffset createdAt)
    {
        if (storeId == Guid.Empty)
        {
            throw new DomainRuleException("store-required", "Customer store is required.");
        }

        return new Customer(Guid.NewGuid(), storeId, name, phone, createdAt);
    }

    private static string NormalizeRequired(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > MaxNameLength)
        {
            throw new DomainRuleException(
                "customer-name-required",
                $"Customer name is required and must not exceed {MaxNameLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizePhone(string? value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > MaxPhoneLength)
        {
            throw new DomainRuleException(
                "invalid-customer-phone",
                $"Customer phone must not exceed {MaxPhoneLength} characters.");
        }

        return normalized;
    }
}
