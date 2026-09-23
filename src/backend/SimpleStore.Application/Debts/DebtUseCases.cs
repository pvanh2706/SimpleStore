using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Stores;
using SimpleStore.Application.Suppliers;
using SimpleStore.Domain;
using SimpleStore.Domain.Debts;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Purchases;

namespace SimpleStore.Application.Debts;

public sealed class GetCustomerDebtUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice5Repository repository,
    TimeProvider timeProvider)
{
    public async Task<DebtBalanceResult> ExecuteAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(currentUser, slice1Repository, cancellationToken);
        var debt = await repository.GetCurrentCustomerDebtAsync(storeId, customerId, cancellationToken)
            ?? throw new ApplicationNotFoundException("customer-not-found", "Customer was not found.");
        EnsureValid(debt.OutstandingAmount, "customer-debt-state-invalid");
        return ToResult(debt, timeProvider.GetUtcNow());
    }

    internal static DebtBalanceResult ToResult(DebtPartyBalance item, DateTimeOffset asOf) =>
        new(item.PartyId, item.PartyName, item.OutstandingAmount, asOf);

    internal static void EnsureValid(decimal outstanding, string code)
    {
        if (outstanding < 0)
        {
            throw new ApplicationConflictException(code, "Debt history produces a negative outstanding balance.");
        }
    }
}

public sealed class GetCustomerDebtsUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice5Repository repository,
    TimeProvider timeProvider)
{
    public async Task<DebtBalanceListResult> ExecuteAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        PaginationGuard.Validate(page, pageSize);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(currentUser, slice1Repository, cancellationToken);
        var result = await repository.SearchCurrentCustomerDebtsAsync(
            storeId, search?.Trim(), page, pageSize, cancellationToken);
        return ToList(result, page, pageSize, timeProvider.GetUtcNow());
    }

    internal static DebtBalanceListResult ToList(
        DebtPartyBalancePage result,
        int page,
        int pageSize,
        DateTimeOffset asOf) =>
        new(
            result.Items.Select(item => GetCustomerDebtUseCase.ToResult(item, asOf)).ToArray(),
            page,
            pageSize,
            result.TotalCount,
            result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)pageSize),
            asOf);
}

public sealed class GetSupplierDebtUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice5Repository repository,
    TimeProvider timeProvider)
{
    public async Task<DebtBalanceResult> ExecuteAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(currentUser, slice1Repository, cancellationToken);
        var debt = await repository.GetCurrentSupplierDebtAsync(storeId, supplierId, cancellationToken)
            ?? throw new ApplicationNotFoundException("supplier-not-found", "Supplier was not found.");
        GetCustomerDebtUseCase.EnsureValid(debt.OutstandingAmount, "supplier-debt-state-invalid");
        return GetCustomerDebtUseCase.ToResult(debt, timeProvider.GetUtcNow());
    }
}

public sealed class GetSupplierDebtsUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice5Repository repository,
    TimeProvider timeProvider)
{
    public async Task<DebtBalanceListResult> ExecuteAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        PaginationGuard.Validate(page, pageSize);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(currentUser, slice1Repository, cancellationToken);
        var result = await repository.SearchCurrentSupplierDebtsAsync(
            storeId, search?.Trim(), page, pageSize, cancellationToken);
        return GetCustomerDebtsUseCase.ToList(result, page, pageSize, timeProvider.GetUtcNow());
    }
}

public sealed class RecordCustomerDebtPaymentUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice5Repository repository,
    TimeProvider timeProvider)
{
    public Task<DebtPaymentResult> ExecuteAsync(
        Guid customerId,
        DebtPaymentCommand command,
        CancellationToken cancellationToken) =>
        DebtPaymentUseCaseSupport.RecordAsync(
            currentUser,
            slice1Repository,
            repository,
            timeProvider,
            customerId,
            command,
            isCustomer: true,
            cancellationToken);
}

public sealed class RecordSupplierDebtPaymentUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice5Repository repository,
    TimeProvider timeProvider)
{
    public Task<DebtPaymentResult> ExecuteAsync(
        Guid supplierId,
        DebtPaymentCommand command,
        CancellationToken cancellationToken) =>
        DebtPaymentUseCaseSupport.RecordAsync(
            currentUser,
            slice1Repository,
            repository,
            timeProvider,
            supplierId,
            command,
            isCustomer: false,
            cancellationToken);
}

internal static class DebtPaymentUseCaseSupport
{
    public static async Task<DebtPaymentResult> RecordAsync(
        ICurrentUser currentUser,
        ISlice1Repository slice1Repository,
        ISlice5Repository repository,
        TimeProvider timeProvider,
        Guid partyId,
        DebtPaymentCommand command,
        bool isCustomer,
        CancellationToken cancellationToken)
    {
        if (command.OperationId == Guid.Empty)
        {
            throw Validation("operation-id-required", "operationId", "OperationId is required.");
        }

        if (command.Amount <= 0)
        {
            throw Validation("invalid-payment-amount", "amount", "Payment amount must be greater than zero.");
        }

        if (decimal.Round(command.Amount, 2) != command.Amount)
        {
            throw Validation("invalid-payment-precision", "amount", "Payment amount supports at most two decimal places.");
        }

        if (command.ExpectedOutstandingAmount < 0
            || decimal.Round(command.ExpectedOutstandingAmount, 2) != command.ExpectedOutstandingAmount)
        {
            throw Validation(
                "invalid-expected-outstanding-amount",
                "expectedOutstandingAmount",
                "Expected outstanding amount must be nonnegative with at most two decimal places.");
        }

        if (!Enum.TryParse<PaymentMethod>(command.Method?.Trim(), true, out var method)
            || !Enum.IsDefined(method))
        {
            throw Validation("invalid-payment-method", "method", "Use Cash or Transfer.");
        }

        string? note;
        try
        {
            note = DebtPayment.NormalizeNote(command.Note);
        }
        catch (DomainRuleException exception)
        {
            throw Validation(exception.Code, "note", exception.Message);
        }

        var userId = CurrentUserGuard.GetRequiredUserId(currentUser);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(currentUser, slice1Repository, cancellationToken);
        var operationType = isCustomer
            ? BusinessOperationTypes.RecordCustomerDebtPayment
            : BusinessOperationTypes.RecordSupplierDebtPayment;
        var fingerprint = Fingerprint(
            operationType,
            storeId,
            partyId,
            command.Amount,
            method,
            command.ExpectedOutstandingAmount,
            note);

        return await repository.ExecuteInTransactionAsync(async token =>
        {
            await repository.AcquireOperationLockAsync(command.OperationId, token);
            var existing = await repository.GetOperationAsync(storeId, command.OperationId, token);
            if (existing is not null)
            {
                if (existing.OperationType != operationType
                    || !string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
                {
                    throw new ApplicationConflictException(
                        "idempotency-key-reused",
                        "OperationId was already used for a different request.");
                }

                var existingPayment = await repository.GetDebtPaymentAsync(
                    storeId,
                    existing.ResultReference!.Value,
                    token)
                    ?? throw new ApplicationConflictException(
                        "debt-payment-result-missing",
                        "Completed debt payment result was not found.");
                return ToResult(
                    existingPayment,
                    partyId,
                    command.ExpectedOutstandingAmount,
                    command.ExpectedOutstandingAmount - command.Amount,
                    true);
            }

            if (isCustomer)
            {
                await repository.AcquireCustomerDebtLockAsync(storeId, partyId, token);
            }
            else
            {
                await repository.AcquireSupplierDebtLockAsync(storeId, partyId, token);
            }

            var now = timeProvider.GetUtcNow();
            var debt = isCustomer
                ? await repository.GetCurrentCustomerDebtAsync(storeId, partyId, token)
                : await repository.GetCurrentSupplierDebtAsync(storeId, partyId, token);
            if (debt is null)
            {
                throw new ApplicationNotFoundException(
                    isCustomer ? "customer-not-found" : "supplier-not-found",
                    isCustomer ? "Customer was not found." : "Supplier was not found.");
            }

            GetCustomerDebtUseCase.EnsureValid(
                debt.OutstandingAmount,
                isCustomer ? "customer-debt-state-invalid" : "supplier-debt-state-invalid");
            if (debt.OutstandingAmount != command.ExpectedOutstandingAmount)
            {
                throw new ApplicationConflictException(
                    isCustomer ? "customer-debt-changed" : "supplier-debt-changed",
                    "Outstanding debt changed. Reload before recording a payment.",
                    new Dictionary<string, object?>
                    {
                        ["latestOutstandingAmount"] = debt.OutstandingAmount
                    });
            }

            if (debt.OutstandingAmount == 0)
            {
                throw new ApplicationConflictException(
                    isCustomer ? "customer-has-no-outstanding-debt" : "supplier-has-no-outstanding-debt",
                    "The party has no outstanding debt.");
            }

            if (command.Amount > debt.OutstandingAmount)
            {
                throw new ApplicationConflictException(
                    "debt-payment-exceeds-outstanding",
                    "Payment cannot exceed the current outstanding debt.",
                    new Dictionary<string, object?>
                    {
                        ["latestOutstandingAmount"] = debt.OutstandingAmount
                    });
            }

            var payment = isCustomer
                ? DebtPayment.RecordCustomerCollection(
                    storeId, command.OperationId, partyId, command.Amount, method, note, now, userId)
                : DebtPayment.RecordSupplierSettlement(
                    storeId, command.OperationId, partyId, command.Amount, method, note, now, userId);
            repository.AddDebtPayment(payment);
            repository.AddBusinessOperation(isCustomer
                ? BusinessOperation.RecordCustomerDebtPayment(
                    command.OperationId, storeId, fingerprint, payment.Id, now)
                : BusinessOperation.RecordSupplierDebtPayment(
                    command.OperationId, storeId, fingerprint, payment.Id, now));
            return ToResult(
                payment,
                partyId,
                debt.OutstandingAmount,
                debt.OutstandingAmount - command.Amount,
                false);
        }, cancellationToken);
    }

    private static ApplicationValidationException Validation(string code, string field, string message) =>
        new(code, message, [new ValidationError(null, field, code, message)]);

    private static string Fingerprint(
        string operationType,
        Guid storeId,
        Guid partyId,
        decimal amount,
        PaymentMethod method,
        decimal expectedOutstanding,
        string? note)
    {
        var normalized = string.Create(
            CultureInfo.InvariantCulture,
            $"{operationType}|store:{storeId:N}|party:{partyId:N}|amount:{amount:G29}|method:{method}|expected:{expectedOutstanding:G29}|note:{note ?? "null"}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static DebtPaymentResult ToResult(
        DebtPayment payment,
        Guid partyId,
        decimal before,
        decimal after,
        bool retry) =>
        new(
            payment.Id,
            partyId,
            payment.Direction.ToString(),
            payment.Purpose.ToString(),
            payment.Amount,
            payment.Method.ToString(),
            payment.Note,
            payment.OccurredAt,
            payment.PerformedByUserId,
            before,
            after,
            retry);
}
