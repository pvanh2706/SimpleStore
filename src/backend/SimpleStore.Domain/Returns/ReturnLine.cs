namespace SimpleStore.Domain.Returns;

public sealed class ReturnLine
{
    private ReturnLine() { }

    private ReturnLine(Guid id, Guid storeId, Guid returnId, ReturnLineInput input)
    {
        if (input.OriginalSaleLineId == Guid.Empty || input.ProductId == Guid.Empty)
        {
            throw new DomainRuleException("return-line-not-from-sale", "An original sale line is required.");
        }

        if (input.Quantity <= 0 || decimal.Round(input.Quantity, 3) != input.Quantity)
        {
            throw new DomainRuleException("invalid-return-quantity", "Return quantity must be positive and have at most three decimal places.");
        }

        if (input.UnitSalePriceBasis < 0 || input.ReturnLineAmount < 0
            || input.UnitCostBasis < 0 || input.RestockedInventoryValue < 0)
        {
            throw new DomainRuleException("invalid-return-value", "Return values cannot be negative.");
        }

        if (!input.Restock && input.RestockedInventoryValue != 0)
        {
            throw new DomainRuleException("invalid-return-restock-value", "No-restock lines cannot restore inventory value.");
        }

        Id = id;
        StoreId = storeId;
        ReturnId = returnId;
        OriginalSaleLineId = input.OriginalSaleLineId;
        ProductId = input.ProductId;
        Quantity = input.Quantity;
        Restock = input.Restock;
        UnitSalePriceBasis = input.UnitSalePriceBasis;
        ReturnLineAmount = input.ReturnLineAmount;
        UnitCostBasis = input.UnitCostBasis;
        RestockedInventoryValue = input.RestockedInventoryValue;
    }

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid ReturnId { get; private set; }
    public Guid OriginalSaleLineId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public bool Restock { get; private set; }
    public decimal UnitSalePriceBasis { get; private set; }
    public decimal ReturnLineAmount { get; private set; }
    public decimal UnitCostBasis { get; private set; }
    public decimal RestockedInventoryValue { get; private set; }

    internal static ReturnLine Create(Guid storeId, Guid returnId, ReturnLineInput input) =>
        new(Guid.NewGuid(), storeId, returnId, input);
}

public sealed record ReturnLineInput(
    Guid OriginalSaleLineId,
    Guid ProductId,
    decimal Quantity,
    bool Restock,
    decimal UnitSalePriceBasis,
    decimal ReturnLineAmount,
    decimal UnitCostBasis,
    decimal RestockedInventoryValue);
