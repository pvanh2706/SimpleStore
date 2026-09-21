namespace SimpleStore.Domain.ProductImports;

public sealed class ProductImport
{
    private readonly List<ProductImportRow> _rows = [];

    private ProductImport()
    {
    }

    private ProductImport(
        Guid id,
        Guid storeId,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        IEnumerable<ProductImportRow> rows)
    {
        Id = id;
        StoreId = storeId;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
        Status = ProductImportStatus.Validated;
        _rows.AddRange(rows);
    }

    public Guid Id { get; private set; }

    public Guid StoreId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public ProductImportStatus Status { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public int ImportedProductCount { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<ProductImportRow> Rows => _rows;

    public static ProductImport CreateValidated(
        Guid storeId,
        Guid createdByUserId,
        DateTimeOffset createdAt,
        IReadOnlyCollection<ProductImportRow> rows)
    {
        if (rows.Count == 0)
        {
            throw new DomainRuleException("import-empty", "A validated import requires at least one row.");
        }

        return new ProductImport(Guid.NewGuid(), storeId, createdByUserId, createdAt, rows);
    }

    public void MarkCompleted(int importedProductCount, DateTimeOffset completedAt)
    {
        if (Status == ProductImportStatus.Completed)
        {
            return;
        }

        Status = ProductImportStatus.Completed;
        ImportedProductCount = importedProductCount;
        CompletedAt = completedAt;
    }
}
