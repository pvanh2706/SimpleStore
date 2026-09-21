namespace SimpleStore.Domain.Sales;

public sealed class Sale
{
    private readonly List<SaleLine> _lines = [];
    private readonly List<SalePayment> _payments = [];

    private Sale()
    {
    }

    private Sale(
        Guid id,
        Guid storeId,
        Guid warehouseId,
        Guid? customerId,
        Guid completedByUserId,
        DateTimeOffset completedAt)
    {
        Id = id;
        StoreId = storeId;
        WarehouseId = warehouseId;
        CustomerId = customerId;
        CompletedByUserId = completedByUserId;
        Status = SaleStatus.Completed;
        CreatedAt = completedAt;
        CompletedAt = completedAt;
    }

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid CompletedByUserId { get; private set; }
    public SaleStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset CompletedAt { get; private set; }
    public decimal TotalAmount { get; private set; }
    public IReadOnlyCollection<SaleLine> Lines => _lines;
    public IReadOnlyCollection<SalePayment> Payments => _payments;
    public decimal PaidAmount => _payments.Sum(payment => payment.Amount);
    public decimal OutstandingAmount => TotalAmount - PaidAmount;

    public static Sale Complete(
        Guid storeId,
        Guid warehouseId,
        Guid? customerId,
        Guid completedByUserId,
        IReadOnlyCollection<SaleLineInput> lines,
        IReadOnlyCollection<SalePaymentInput> payments,
        DateTimeOffset completedAt)
    {
        if (storeId == Guid.Empty || warehouseId == Guid.Empty || completedByUserId == Guid.Empty)
        {
            throw new DomainRuleException("sale-context-required", "Sale store, warehouse and cashier are required.");
        }

        if (lines.Count == 0)
        {
            throw new DomainRuleException("sale-lines-required", "A sale requires at least one line.");
        }

        if (lines.GroupBy(line => line.ProductId).Any(group => group.Count() > 1))
        {
            throw new DomainRuleException("duplicate-sale-product", "A product can appear only once in a sale.");
        }

        var sale = new Sale(
            Guid.NewGuid(),
            storeId,
            warehouseId,
            customerId,
            completedByUserId,
            completedAt);
        sale._lines.AddRange(lines.Select(line => SaleLine.Create(storeId, sale.Id, line)));
        sale.TotalAmount = sale._lines.Sum(line => line.LineAmount);
        sale._payments.AddRange(payments.Select(payment => SalePayment.Create(
            storeId,
            sale.Id,
            payment,
            completedAt,
            completedByUserId)));

        if (sale.PaidAmount > sale.TotalAmount)
        {
            throw new DomainRuleException("sale-overpayment", "Payments cannot exceed the sale total.");
        }

        if (sale.OutstandingAmount > 0 && customerId is null)
        {
            throw new DomainRuleException(
                "customer-required-for-credit",
                "A customer is required when the sale has an outstanding amount.");
        }

        return sale;
    }
}
