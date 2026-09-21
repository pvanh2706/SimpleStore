namespace SimpleStore.Domain.Purchases;

public sealed class PurchaseLine
{
    private PurchaseLine()
    {
    }

    private PurchaseLine(
        Guid id,
        Guid storeId,
        Guid purchaseId,
        Guid productId,
        decimal quantity,
        decimal unitPrice)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainRuleException("product-required", "A product is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainRuleException("invalid-purchase-quantity", "Purchase quantity must be greater than zero.");
        }

        if (decimal.Round(quantity, 3) != quantity)
        {
            throw new DomainRuleException(
                "invalid-purchase-quantity-precision",
                "Purchase quantity supports at most three decimal places.");
        }

        if (unitPrice < 0)
        {
            throw new DomainRuleException("invalid-purchase-unit-price", "Purchase unit price cannot be negative.");
        }
        if (decimal.Round(unitPrice, 2) != unitPrice)
        {
            throw new DomainRuleException(
                "invalid-purchase-unit-price-precision",
                "Purchase unit price supports at most two decimal places.");
        }

        Id = id;
        StoreId = storeId;
        PurchaseId = purchaseId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
        LineAmount = CalculateLineAmount(quantity, unitPrice);
    }

    public Guid Id { get; private set; }

    public Guid StoreId { get; private set; }

    public Guid PurchaseId { get; private set; }

    public Guid ProductId { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineAmount { get; private set; }

    internal static PurchaseLine Create(
        Guid storeId,
        Guid purchaseId,
        Guid productId,
        decimal quantity,
        decimal unitPrice) =>
        new(Guid.NewGuid(), storeId, purchaseId, productId, quantity, unitPrice);

    public static decimal CalculateLineAmount(decimal quantity, decimal unitPrice) =>
        Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
}

public sealed record PurchaseLineInput(Guid ProductId, decimal Quantity, decimal UnitPrice);
