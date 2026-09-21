namespace SimpleStore.Domain.Suppliers;

public sealed class Supplier
{
    public const int MaxNameLength = 160;
    public const int MaxPhoneLength = 32;
    public const int MaxNoteLength = 500;

    private Supplier()
    {
    }

    private Supplier(
        Guid id,
        Guid storeId,
        string name,
        string? phone,
        string? note,
        DateTimeOffset createdAt)
    {
        Id = id;
        StoreId = storeId;
        SetDetails(name, phone, note);
        IsActive = true;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid StoreId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public string? Note { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static Supplier Create(
        Guid storeId,
        string name,
        string? phone,
        string? note,
        DateTimeOffset createdAt)
    {
        if (storeId == Guid.Empty)
        {
            throw new DomainRuleException("store-required", "Supplier store is required.");
        }

        return new Supplier(Guid.NewGuid(), storeId, name, phone, note, createdAt);
    }

    public void Update(string name, string? phone, string? note, DateTimeOffset updatedAt)
    {
        SetDetails(name, phone, note);
        UpdatedAt = updatedAt;
    }

    public void Deactivate(DateTimeOffset updatedAt)
    {
        IsActive = false;
        UpdatedAt = updatedAt;
    }

    private void SetDetails(string name, string? phone, string? note)
    {
        Name = NormalizeRequired(name, MaxNameLength, "supplier-name-required");
        Phone = NormalizeOptional(phone, MaxPhoneLength, "invalid-supplier-phone");
        Note = NormalizeOptional(note, MaxNoteLength, "invalid-supplier-note");
    }

    private static string NormalizeRequired(string? value, int maxLength, string code)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 || normalized.Length > maxLength)
        {
            throw new DomainRuleException(code, $"Value is required and must not exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string code)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainRuleException(code, $"Value must not exceed {maxLength} characters.");
        }

        return normalized;
    }
}
