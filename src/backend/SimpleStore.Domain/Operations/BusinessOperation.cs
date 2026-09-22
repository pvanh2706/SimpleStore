namespace SimpleStore.Domain.Operations;

public sealed class BusinessOperation
{
    public const int MaxOperationTypeLength = 64;
    public const int FingerprintLength = 64;

    private BusinessOperation()
    {
    }

    private BusinessOperation(
        Guid operationId,
        Guid storeId,
        string operationType,
        string requestFingerprint,
        Guid resultReference,
        DateTimeOffset createdAt)
    {
        OperationId = operationId;
        StoreId = storeId;
        OperationType = operationType;
        RequestFingerprint = requestFingerprint;
        Status = BusinessOperationStatus.Completed;
        ResultReference = resultReference;
        CreatedAt = createdAt;
        CompletedAt = createdAt;
    }

    public Guid OperationId { get; private set; }

    public Guid StoreId { get; private set; }

    public string OperationType { get; private set; } = string.Empty;

    public string RequestFingerprint { get; private set; } = string.Empty;

    public BusinessOperationStatus Status { get; private set; }

    public Guid? ResultReference { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static BusinessOperation CompletePurchase(
        Guid operationId,
        Guid storeId,
        string requestFingerprint,
        Guid purchaseId,
        DateTimeOffset completedAt)
    {
        if (operationId == Guid.Empty)
        {
            throw new DomainRuleException("operation-id-required", "OperationId is required.");
        }

        return new BusinessOperation(
            operationId,
            storeId,
            BusinessOperationTypes.CompletePurchase,
            requestFingerprint,
            purchaseId,
            completedAt);
    }

    public static BusinessOperation CompleteSale(
        Guid operationId,
        Guid storeId,
        string requestFingerprint,
        Guid saleId,
        DateTimeOffset completedAt)
    {
        if (operationId == Guid.Empty)
        {
            throw new DomainRuleException("operation-id-required", "OperationId is required.");
        }

        return new BusinessOperation(
            operationId,
            storeId,
            BusinessOperationTypes.CompleteSale,
            requestFingerprint,
            saleId,
            completedAt);
    }

    public static BusinessOperation CreateReturn(
        Guid operationId,
        Guid storeId,
        string requestFingerprint,
        Guid returnId,
        DateTimeOffset completedAt) =>
        Complete(operationId, storeId, BusinessOperationTypes.CreateReturn, requestFingerprint, returnId, completedAt);

    public static BusinessOperation VoidSale(
        Guid operationId,
        Guid storeId,
        string requestFingerprint,
        Guid saleVoidId,
        DateTimeOffset completedAt) =>
        Complete(operationId, storeId, BusinessOperationTypes.VoidSale, requestFingerprint, saleVoidId, completedAt);

    public static BusinessOperation VoidPurchase(
        Guid operationId,
        Guid storeId,
        string requestFingerprint,
        Guid purchaseVoidId,
        DateTimeOffset completedAt) =>
        Complete(operationId, storeId, BusinessOperationTypes.VoidPurchase, requestFingerprint, purchaseVoidId, completedAt);

    public static BusinessOperation RecordCustomerDebtPayment(
        Guid operationId,
        Guid storeId,
        string requestFingerprint,
        Guid debtPaymentId,
        DateTimeOffset completedAt) =>
        Complete(operationId, storeId, BusinessOperationTypes.RecordCustomerDebtPayment, requestFingerprint, debtPaymentId, completedAt);

    public static BusinessOperation RecordSupplierDebtPayment(
        Guid operationId,
        Guid storeId,
        string requestFingerprint,
        Guid debtPaymentId,
        DateTimeOffset completedAt) =>
        Complete(operationId, storeId, BusinessOperationTypes.RecordSupplierDebtPayment, requestFingerprint, debtPaymentId, completedAt);

    private static BusinessOperation Complete(
        Guid operationId,
        Guid storeId,
        string operationType,
        string requestFingerprint,
        Guid resultReference,
        DateTimeOffset completedAt)
    {
        if (operationId == Guid.Empty)
        {
            throw new DomainRuleException("operation-id-required", "OperationId is required.");
        }

        return new BusinessOperation(
            operationId,
            storeId,
            operationType,
            requestFingerprint,
            resultReference,
            completedAt);
    }
}

public enum BusinessOperationStatus
{
    Processing = 1,
    Completed = 2
}

public static class BusinessOperationTypes
{
    public const string CompletePurchase = "CompletePurchase";
    public const string CompleteSale = "CompleteSale";
    public const string CreateReturn = "CreateReturn";
    public const string VoidSale = "VoidSale";
    public const string VoidPurchase = "VoidPurchase";
    public const string RecordCustomerDebtPayment = "RecordCustomerDebtPayment";
    public const string RecordSupplierDebtPayment = "RecordSupplierDebtPayment";
}
