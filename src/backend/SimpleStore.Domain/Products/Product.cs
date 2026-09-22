namespace SimpleStore.Domain.Products;

public sealed class Product
{
    public const int MaxSkuLength = 64;
    public const int MaxBarcodeLength = 64;
    public const int MaxNameLength = 160;
    public const int MaxUnitLength = 32;

    private Product()
    {
    }

    private Product(
        Guid id,
        Guid storeId,
        string sku,
        string? barcode,
        string name,
        string unit,
        decimal salePrice,
        decimal? referencePurchaseCost,
        DateTimeOffset createdAt)
    {
        Id = id;
        StoreId = storeId;
        SetDetails(sku, barcode, name, unit, salePrice, referencePurchaseCost);
        IsActive = true;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid StoreId { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string NormalizedSku { get; private set; } = string.Empty;

    public string? Barcode { get; private set; }

    public string? NormalizedBarcode { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Unit { get; private set; } = string.Empty;

    public decimal SalePrice { get; private set; }

    public decimal? ReferencePurchaseCost { get; private set; }

    public long ReferencePurchaseCostRevision { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public static Product Create(
        Guid storeId,
        string sku,
        string? barcode,
        string name,
        string unit,
        decimal salePrice,
        decimal? referencePurchaseCost,
        DateTimeOffset createdAt)
    {
        if (storeId == Guid.Empty)
        {
            throw new DomainRuleException("store-required", "Product store is required.");
        }

        return new Product(
            Guid.NewGuid(),
            storeId,
            sku,
            barcode,
            name,
            unit,
            salePrice,
            referencePurchaseCost,
            createdAt);
    }

    public void Update(
        string sku,
        string? barcode,
        string name,
        string unit,
        decimal salePrice,
        decimal? referencePurchaseCost,
        DateTimeOffset updatedAt)
    {
        SetDetails(sku, barcode, name, unit, salePrice, referencePurchaseCost);
        UpdatedAt = updatedAt;
    }

    public void Deactivate(DateTimeOffset updatedAt)
    {
        IsActive = false;
        UpdatedAt = updatedAt;
    }

    public void UpdateReferencePurchaseCost(decimal unitPrice, DateTimeOffset updatedAt)
    {
        if (unitPrice < 0)
        {
            throw new DomainRuleException(
                "invalid-reference-purchase-cost",
                "Reference purchase cost cannot be negative.");
        }

        SetReferencePurchaseCost(unitPrice);
        UpdatedAt = updatedAt;
    }

    private void SetDetails(
        string sku,
        string? barcode,
        string name,
        string unit,
        decimal salePrice,
        decimal? referencePurchaseCost)
    {
        Sku = RequireText(sku, MaxSkuLength, "sku-required", "SKU is required.");
        NormalizedSku = NormalizeIdentifier(Sku);

        Barcode = NormalizeOptionalText(barcode, MaxBarcodeLength, "invalid-barcode");
        NormalizedBarcode = Barcode is null ? null : NormalizeIdentifier(Barcode);

        Name = RequireText(name, MaxNameLength, "name-required", "Product name is required.");
        Unit = RequireText(unit, MaxUnitLength, "unit-required", "Product unit is required.");

        if (salePrice < 0)
        {
            throw new DomainRuleException("invalid-sale-price", "Sale price cannot be negative.");
        }

        if (referencePurchaseCost < 0)
        {
            throw new DomainRuleException(
                "invalid-reference-purchase-cost",
                "Reference purchase cost cannot be negative.");
        }

        SalePrice = salePrice;
        SetReferencePurchaseCost(referencePurchaseCost);
    }

    public void RestoreReferencePurchaseCost(decimal? referencePurchaseCost, DateTimeOffset updatedAt)
    {
        if (referencePurchaseCost < 0)
        {
            throw new DomainRuleException(
                "invalid-reference-purchase-cost",
                "Reference purchase cost cannot be negative.");
        }

        SetReferencePurchaseCost(referencePurchaseCost);
        UpdatedAt = updatedAt;
    }

    private void SetReferencePurchaseCost(decimal? value)
    {
        if (ReferencePurchaseCost != value)
        {
            ReferencePurchaseCost = value;
            ReferencePurchaseCostRevision++;
        }
    }

    private static string RequireText(string? value, int maxLength, string code, string message)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 || normalized.Length > maxLength)
        {
            throw new DomainRuleException(code, message);
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string code)
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

    public static string NormalizeIdentifier(string value) => value.Trim().ToUpperInvariant();
}
