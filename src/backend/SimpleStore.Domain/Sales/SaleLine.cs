using SimpleStore.Domain.Inventory;

namespace SimpleStore.Domain.Sales;

public sealed class SaleLine
{
    private SaleLine()
    {
    }

    private SaleLine(
        Guid id,
        Guid storeId,
        Guid saleId,
        SaleLineInput input)
    {
        if (input.ProductId == Guid.Empty)
        {
            throw new DomainRuleException("product-required", "A product is required.");
        }

        if (input.Quantity <= 0 || decimal.Round(input.Quantity, 3) != input.Quantity)
        {
            throw new DomainRuleException(
                "invalid-sale-quantity",
                "Sale quantity must be greater than zero and support at most three decimal places.");
        }

        if (input.UnitSalePrice < 0 || decimal.Round(input.UnitSalePrice, 2) != input.UnitSalePrice)
        {
            throw new DomainRuleException(
                "invalid-sale-price",
                "Sale price cannot be negative and supports at most two decimal places.");
        }

        if (input.UnitCostAtSale < 0 || decimal.Round(input.UnitCostAtSale, 4) != input.UnitCostAtSale)
        {
            throw new DomainRuleException(
                "invalid-sale-cost",
                "Sale cost cannot be negative and supports at most four decimal places.");
        }

        var name = input.ProductName?.Trim() ?? string.Empty;
        var sku = input.ProductSku?.Trim() ?? string.Empty;
        var unit = input.ProductUnit?.Trim() ?? string.Empty;
        if (name.Length is < 1 or > 160 || sku.Length is < 1 or > 64 || unit.Length is < 1 or > 32)
        {
            throw new DomainRuleException("invalid-sale-product-snapshot", "Product snapshot is invalid.");
        }

        Id = id;
        StoreId = storeId;
        SaleId = saleId;
        ProductId = input.ProductId;
        ProductName = name;
        ProductSku = sku;
        ProductUnit = unit;
        Quantity = input.Quantity;
        UnitSalePrice = input.UnitSalePrice;
        LineAmount = CalculateLineAmount(input.Quantity, input.UnitSalePrice);
        UnitCostAtSale = input.UnitCostAtSale;
        CostReliability = input.CostReliability;
    }

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductUnit { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitSalePrice { get; private set; }
    public decimal LineAmount { get; private set; }
    public decimal UnitCostAtSale { get; private set; }
    public CostReliability CostReliability { get; private set; }

    internal static SaleLine Create(Guid storeId, Guid saleId, SaleLineInput input) =>
        new(Guid.NewGuid(), storeId, saleId, input);

    public static decimal CalculateLineAmount(decimal quantity, decimal unitSalePrice) =>
        Math.Round(quantity * unitSalePrice, 2, MidpointRounding.AwayFromZero);
}

public sealed record SaleLineInput(
    Guid ProductId,
    string ProductName,
    string ProductSku,
    string ProductUnit,
    decimal Quantity,
    decimal UnitSalePrice,
    decimal UnitCostAtSale,
    CostReliability CostReliability);
