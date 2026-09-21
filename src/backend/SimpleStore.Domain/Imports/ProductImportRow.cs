namespace SimpleStore.Domain.ProductImports;

public sealed class ProductImportRow
{
    private ProductImportRow()
    {
    }

    private ProductImportRow(
        Guid id,
        int rowNumber,
        string sku,
        string? barcode,
        string name,
        string unit,
        decimal salePrice,
        decimal? openingCost,
        decimal openingQuantity)
    {
        Id = id;
        RowNumber = rowNumber;
        Sku = sku;
        Barcode = barcode;
        Name = name;
        Unit = unit;
        SalePrice = salePrice;
        OpeningCost = openingCost;
        OpeningQuantity = openingQuantity;
    }

    public Guid Id { get; private set; }

    public Guid ProductImportId { get; private set; }

    public int RowNumber { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string? Barcode { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Unit { get; private set; } = string.Empty;

    public decimal SalePrice { get; private set; }

    public decimal? OpeningCost { get; private set; }

    public decimal OpeningQuantity { get; private set; }

    public static ProductImportRow Create(
        int rowNumber,
        string sku,
        string? barcode,
        string name,
        string unit,
        decimal salePrice,
        decimal? openingCost,
        decimal openingQuantity) =>
        new(
            Guid.NewGuid(),
            rowNumber,
            sku,
            barcode,
            name,
            unit,
            salePrice,
            openingCost,
            openingQuantity);
}
