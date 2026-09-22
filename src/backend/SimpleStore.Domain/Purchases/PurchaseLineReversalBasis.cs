namespace SimpleStore.Domain.Purchases;

public sealed class PurchaseLineReversalBasis
{
    private PurchaseLineReversalBasis() { }

    private PurchaseLineReversalBasis(
        Guid id,
        Guid storeId,
        Guid purchaseId,
        Guid purchaseLineId,
        Guid productId,
        Guid warehouseId,
        decimal quantityBefore,
        decimal inventoryValueBefore,
        decimal averageCostBefore,
        bool hasAverageCostBefore,
        decimal? referencePurchaseCostBefore,
        decimal referencePurchaseCostApplied,
        long referencePurchaseCostRevisionAfterPurchase,
        Guid purchaseMovementId,
        DateTimeOffset capturedAt)
    {
        Id = id;
        StoreId = storeId;
        PurchaseId = purchaseId;
        PurchaseLineId = purchaseLineId;
        ProductId = productId;
        WarehouseId = warehouseId;
        QuantityBefore = quantityBefore;
        InventoryValueBefore = inventoryValueBefore;
        AverageCostBefore = averageCostBefore;
        HasAverageCostBefore = hasAverageCostBefore;
        ReferencePurchaseCostBefore = referencePurchaseCostBefore;
        ReferencePurchaseCostApplied = referencePurchaseCostApplied;
        ReferencePurchaseCostRevisionAfterPurchase = referencePurchaseCostRevisionAfterPurchase;
        PurchaseMovementId = purchaseMovementId;
        CapturedAt = capturedAt;
    }

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid PurchaseId { get; private set; }
    public Guid PurchaseLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public decimal QuantityBefore { get; private set; }
    public decimal InventoryValueBefore { get; private set; }
    public decimal AverageCostBefore { get; private set; }
    public bool HasAverageCostBefore { get; private set; }
    public decimal? ReferencePurchaseCostBefore { get; private set; }
    public decimal ReferencePurchaseCostApplied { get; private set; }
    public long ReferencePurchaseCostRevisionAfterPurchase { get; private set; }
    public Guid PurchaseMovementId { get; private set; }
    public DateTimeOffset CapturedAt { get; private set; }

    public static PurchaseLineReversalBasis Capture(
        Guid storeId,
        Guid purchaseId,
        Guid purchaseLineId,
        Guid productId,
        Guid warehouseId,
        decimal quantityBefore,
        decimal inventoryValueBefore,
        decimal averageCostBefore,
        bool hasAverageCostBefore,
        decimal? referencePurchaseCostBefore,
        decimal referencePurchaseCostApplied,
        long referencePurchaseCostRevisionAfterPurchase,
        Guid purchaseMovementId,
        DateTimeOffset capturedAt) =>
        new(
            Guid.NewGuid(), storeId, purchaseId, purchaseLineId, productId, warehouseId,
            quantityBefore, inventoryValueBefore, averageCostBefore, hasAverageCostBefore,
            referencePurchaseCostBefore, referencePurchaseCostApplied,
            referencePurchaseCostRevisionAfterPurchase, purchaseMovementId, capturedAt);
}
