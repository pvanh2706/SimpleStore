using SimpleStore.Domain.Purchases;

namespace SimpleStore.Domain.Debts;

public sealed class DebtPayment
{
    public const int MaxNoteLength = 250;

    private DebtPayment()
    {
    }

    private DebtPayment(
        Guid storeId,
        Guid operationId,
        DebtPaymentDirection direction,
        DebtPaymentPurpose purpose,
        Guid? customerId,
        Guid? supplierId,
        decimal amount,
        PaymentMethod method,
        string? note,
        DateTimeOffset occurredAt,
        Guid performedByUserId)
    {
        if (storeId == Guid.Empty || operationId == Guid.Empty || performedByUserId == Guid.Empty)
        {
            throw new DomainRuleException(
                "debt-payment-context-required",
                "Debt payment store, operation and actor are required.");
        }

        if (amount <= 0)
        {
            throw new DomainRuleException("invalid-payment-amount", "Payment amount must be greater than zero.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainRuleException(
                "invalid-payment-precision",
                "Payment amount supports at most two decimal places.");
        }

        if (!Enum.IsDefined(method))
        {
            throw new DomainRuleException("invalid-payment-method", "Payment method is invalid.");
        }

        var isCustomerCollection = purpose == DebtPaymentPurpose.CustomerDebtCollection
            && direction == DebtPaymentDirection.MoneyIn
            && customerId.HasValue
            && customerId.Value != Guid.Empty
            && supplierId is null;
        var isSupplierSettlement = purpose == DebtPaymentPurpose.SupplierDebtSettlement
            && direction == DebtPaymentDirection.MoneyOut
            && supplierId.HasValue
            && supplierId.Value != Guid.Empty
            && customerId is null;
        if (!isCustomerCollection && !isSupplierSettlement)
        {
            throw new DomainRuleException(
                "invalid-debt-payment-party",
                "Debt payment party, purpose and direction are inconsistent.");
        }

        Id = Guid.NewGuid();
        StoreId = storeId;
        OperationId = operationId;
        Direction = direction;
        Purpose = purpose;
        CustomerId = customerId;
        SupplierId = supplierId;
        Amount = amount;
        Method = method;
        Note = NormalizeNote(note);
        OccurredAt = occurredAt;
        PerformedByUserId = performedByUserId;
    }

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid OperationId { get; private set; }
    public DebtPaymentDirection Direction { get; private set; }
    public DebtPaymentPurpose Purpose { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid PerformedByUserId { get; private set; }

    public static DebtPayment RecordCustomerCollection(
        Guid storeId,
        Guid operationId,
        Guid customerId,
        decimal amount,
        PaymentMethod method,
        string? note,
        DateTimeOffset occurredAt,
        Guid performedByUserId) =>
        new(
            storeId,
            operationId,
            DebtPaymentDirection.MoneyIn,
            DebtPaymentPurpose.CustomerDebtCollection,
            customerId,
            null,
            amount,
            method,
            note,
            occurredAt,
            performedByUserId);

    public static DebtPayment RecordSupplierSettlement(
        Guid storeId,
        Guid operationId,
        Guid supplierId,
        decimal amount,
        PaymentMethod method,
        string? note,
        DateTimeOffset occurredAt,
        Guid performedByUserId) =>
        new(
            storeId,
            operationId,
            DebtPaymentDirection.MoneyOut,
            DebtPaymentPurpose.SupplierDebtSettlement,
            null,
            supplierId,
            amount,
            method,
            note,
            occurredAt,
            performedByUserId);

    public static string? NormalizeNote(string? note)
    {
        var normalized = note?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > MaxNoteLength)
        {
            throw new DomainRuleException(
                "invalid-note-length",
                $"Payment note cannot exceed {MaxNoteLength} characters.");
        }

        return normalized;
    }
}

public enum DebtPaymentDirection
{
    MoneyIn = 1,
    MoneyOut = 2
}

public enum DebtPaymentPurpose
{
    CustomerDebtCollection = 1,
    SupplierDebtSettlement = 2
}
