using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Stores;
using SimpleStore.Application.Suppliers;
using SimpleStore.Domain;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Purchases;
using SimpleStore.Domain.Returns;
using SimpleStore.Domain.Sales;

namespace SimpleStore.Application.Returns;

public sealed class PreviewReturnUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice4Repository repository)
{
    public async Task<ReturnPreviewResult> ExecuteAsync(ReturnPreviewCommand command, CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(currentUser, slice1Repository, cancellationToken);
        var calculation = await ReturnUseCaseSupport.CalculateAsync(repository, storeId, command.OriginalSaleId, command.Lines, cancellationToken);
        return calculation.ToPreview();
    }
}

public sealed class CreateReturnUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice4Repository repository,
    TimeProvider timeProvider)
{
    public async Task<ReturnResult> ExecuteAsync(CreateReturnCommand command, CancellationToken cancellationToken)
    {
        var userId = CurrentUserGuard.GetRequiredUserId(currentUser);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(currentUser, slice1Repository, cancellationToken);
        ReturnUseCaseSupport.ValidateCommandLines(command.Lines);
        var normalizedMethod = ReturnUseCaseSupport.ParseRefundMethod(command.RefundMethod);
        var fingerprint = ReturnUseCaseSupport.CreateFingerprint(command, normalizedMethod);

        return await repository.ExecuteInTransactionAsync(async transactionToken =>
        {
            await repository.AcquireOperationLockAsync(command.OperationId, transactionToken);
            var existing = await repository.GetOperationAsync(storeId, command.OperationId, transactionToken);
            if (existing is not null)
            {
                if (existing.OperationType != BusinessOperationTypes.CreateReturn
                    || !string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal))
                {
                    throw new ApplicationConflictException("idempotency-key-reused", "OperationId was already used for a different request.");
                }

                var existingReturn = await repository.GetReturnAsync(storeId, existing.ResultReference!.Value, transactionToken)
                    ?? throw new ApplicationNotFoundException("return-not-found", "Return was not found.");
                return ReturnUseCaseSupport.ToResult(existingReturn, true);
            }

            await repository.AcquireSaleCorrectionLockAsync(command.OriginalSaleId, transactionToken);
            var calculation = await ReturnUseCaseSupport.CalculateAsync(
                repository, storeId, command.OriginalSaleId, command.Lines, transactionToken);
            if (calculation.Financials.RefundDueNow > 0 && normalizedMethod is null)
            {
                throw new ApplicationValidationException(
                    "refund-method-required", "A refund method is required.",
                    [new ValidationError(null, "refundMethod", "refund-method-required", "Choose Cash or Transfer.")]);
            }
            if (calculation.Financials.RefundDueNow == 0 && normalizedMethod is not null)
            {
                throw new ApplicationValidationException(
                    "refund-method-not-applicable", "Refund method is not applicable when no refund is due.",
                    [new ValidationError(null, "refundMethod", "refund-method-not-applicable", "Remove the refund method.")]);
            }

            var restockProductIds = calculation.Lines.Where(item => item.Command.Restock == true)
                .Select(item => item.SaleLine.ProductId).Distinct().OrderBy(item => item).ToArray();
            var balances = restockProductIds.Length == 0
                ? new Dictionary<Guid, InventoryBalance>()
                : await repository.LockInventoryBalancesAsync(
                    storeId, calculation.Sale.WarehouseId, restockProductIds, transactionToken);
            if (balances.Count != restockProductIds.Length)
            {
                throw new ApplicationConflictException("inventory-balance-missing", "An inventory balance is missing for a return product.");
            }

            var now = timeProvider.GetUtcNow();
            var customerReturn = CustomerReturn.Complete(
                storeId,
                calculation.Sale.Id,
                userId,
                calculation.Lines.Select(item => new ReturnLineInput(
                    item.SaleLine.Id,
                    item.SaleLine.ProductId,
                    item.Command.Quantity,
                    item.Command.Restock!.Value,
                    item.SaleLine.UnitSalePrice,
                    item.Amounts.ReturnLineAmount,
                    item.SaleLine.UnitCostAtSale,
                    item.Amounts.RestockedInventoryValue)).ToArray(),
                calculation.Financials.RefundDueNow,
                normalizedMethod,
                now);
            repository.AddReturn(customerReturn);

            foreach (var line in customerReturn.Lines.Where(item => item.Restock).OrderBy(item => item.ProductId))
            {
                balances[line.ProductId].ReceiveInbound(line.Quantity, line.RestockedInventoryValue, now);
                repository.AddInventoryMovement(InventoryMovement.CreateReturnRestock(
                    storeId,
                    calculation.Sale.WarehouseId,
                    line.ProductId,
                    line.Quantity,
                    line.RestockedInventoryValue,
                    line.UnitCostBasis,
                    line.Id,
                    userId,
                    now));
            }

            repository.AddBusinessOperation(BusinessOperation.CreateReturn(
                command.OperationId, storeId, fingerprint, customerReturn.Id, now));
            return ReturnUseCaseSupport.ToResult(customerReturn, false);
        }, cancellationToken);
    }
}

public sealed class GetReturnUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice4Repository repository)
{
    public async Task<ReturnResult> ExecuteAsync(Guid returnId, CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(currentUser, slice1Repository, cancellationToken);
        var result = await repository.GetReturnAsync(storeId, returnId, cancellationToken)
            ?? throw new ApplicationNotFoundException("return-not-found", "Return was not found.");
        return ReturnUseCaseSupport.ToResult(result, false);
    }
}

public sealed class GetReturnContextUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice4Repository repository)
{
    public async Task<ReturnContextResult> ExecuteAsync(Guid saleId, CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(currentUser, slice1Repository, cancellationToken);
        var sale = await repository.GetSaleAsync(storeId, saleId, cancellationToken)
            ?? throw new ApplicationNotFoundException("return-sale-not-found", "Sale was not found.");
        var returns = await repository.GetReturnsForSaleAsync(storeId, saleId, cancellationToken);
        var saleVoid = await repository.GetSaleVoidAsync(storeId, saleId, cancellationToken);
        var totalReturned = returns.Sum(item => item.TotalReturnAmount);
        var totalRefunded = returns.Sum(item => item.RefundAmount);
        var originalCollected = sale.PaidAmount;
        var netSale = saleVoid is null ? sale.TotalAmount - totalReturned : 0;
        var netCollected = saleVoid is null ? originalCollected - totalRefunded : 0;
        var outstanding = saleVoid is null ? Math.Max(0, netSale - netCollected) : 0;
        var returnedByLine = returns.SelectMany(item => item.Lines).GroupBy(item => item.OriginalSaleLineId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        return new ReturnContextResult(
            sale.Id,
            saleVoid is not null,
            returns.Count > 0,
            sale.TotalAmount,
            totalReturned,
            netSale,
            originalCollected,
            totalRefunded,
            netCollected,
            outstanding,
            sale.Lines.Select(line =>
            {
                var returned = returnedByLine.GetValueOrDefault(line.Id);
                return new ReturnContextLineResult(
                    line.Id, line.ProductId, line.ProductName, line.ProductSku, line.ProductUnit,
                    line.Quantity, returned, line.Quantity - returned, line.UnitSalePrice, line.LineAmount);
            }).ToArray());
    }
}

internal static class ReturnUseCaseSupport
{
    public static void ValidateCommandLines(IReadOnlyCollection<ReturnLineCommand> lines)
    {
        if (lines.Count == 0)
        {
            throw new ApplicationValidationException(
                "return-lines-required", "A return requires at least one line.",
                [new ValidationError(null, "lines", "return-lines-required", "Add at least one sale line.")]);
        }
        if (lines.GroupBy(item => item.OriginalSaleLineId).Any(group => group.Count() > 1))
        {
            throw new ApplicationValidationException(
                "duplicate-return-line", "An original sale line can appear only once.",
                [new ValidationError(null, "lines", "duplicate-return-line", "Remove duplicate sale lines.")]);
        }
        if (lines.Any(item => item.Restock is null))
        {
            throw new ApplicationValidationException(
                "restock-selection-required", "Restock or NoRestock must be selected for every line.",
                [new ValidationError(null, "restock", "restock-selection-required", "Choose Restock or NoRestock.")]);
        }
    }

    public static PaymentMethod? ParseRefundMethod(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!Enum.TryParse<PaymentMethod>(value.Trim(), true, out var result)
            || !Enum.IsDefined(result))
        {
            throw new ApplicationValidationException(
                "invalid-refund-method", "Refund method is invalid.",
                [new ValidationError(null, "refundMethod", "invalid-refund-method", "Use Cash or Transfer.")]);
        }
        return result;
    }

    public static async Task<ReturnCalculation> CalculateAsync(
        ISlice4Repository repository,
        Guid storeId,
        Guid saleId,
        IReadOnlyCollection<ReturnLineCommand> commands,
        CancellationToken cancellationToken)
    {
        ValidateCommandLines(commands);
        var sale = await repository.GetSaleAsync(storeId, saleId, cancellationToken)
            ?? throw new ApplicationNotFoundException("return-sale-not-found", "Sale was not found.");
        if (await repository.GetSaleVoidAsync(storeId, saleId, cancellationToken) is not null)
        {
            throw new ApplicationConflictException("sale-already-voided", "A voided sale cannot be returned.");
        }
        var returns = await repository.GetReturnsForSaleAsync(storeId, saleId, cancellationToken);
        var lineById = sale.Lines.ToDictionary(item => item.Id);
        if (commands.Any(item => !lineById.ContainsKey(item.OriginalSaleLineId)))
        {
            throw new ApplicationValidationException(
                "return-line-not-from-sale", "A return line does not belong to the original sale.",
                [new ValidationError(null, "originalSaleLineId", "return-line-not-from-sale", "Select a line from the original sale.")]);
        }
        var movements = await repository.GetSaleMovementsAsync(storeId, commands.Select(item => item.OriginalSaleLineId).ToArray(), cancellationToken);
        var previousLines = returns.SelectMany(item => item.Lines).ToArray();
        var calculatedLines = new List<CalculatedReturnLine>();
        foreach (var command in commands.OrderBy(item => item.OriginalSaleLineId))
        {
            var line = lineById[command.OriginalSaleLineId];
            if (!movements.TryGetValue(line.Id, out var movement)
                || movement.WarehouseId != sale.WarehouseId || movement.ProductId != line.ProductId
                || movement.SourceId != line.Id || movement.QuantityDelta != -line.Quantity)
            {
                throw new ApplicationConflictException("return-financial-state-invalid", "Original sale inventory evidence is invalid.");
            }
            var expectedValue = Math.Round(line.Quantity * line.UnitCostAtSale, 2, MidpointRounding.AwayFromZero);
            if (movement.InventoryValueDelta != -expectedValue)
            {
                throw new ApplicationConflictException("return-financial-state-invalid", "Original sale inventory value evidence is invalid.");
            }
            var previous = previousLines.Where(item => item.OriginalSaleLineId == line.Id).ToArray();
            ReturnLineAmounts amounts;
            try
            {
                amounts = ReturnCalculations.CalculateLine(
                    line.Quantity, line.LineAmount, line.UnitSalePrice, Math.Abs(movement.InventoryValueDelta),
                    line.UnitCostAtSale, previous.Sum(item => item.Quantity), previous.Sum(item => item.ReturnLineAmount),
                    previous.Where(item => item.Restock).Sum(item => item.Quantity),
                    previous.Where(item => item.Restock).Sum(item => item.RestockedInventoryValue),
                    command.Quantity, command.Restock!.Value);
            }
            catch (DomainRuleException exception) when (exception.Code is "return-quantity-exceeds-remaining" or "return-financial-state-invalid")
            {
                throw new ApplicationConflictException(exception.Code, exception.Message);
            }
            calculatedLines.Add(new CalculatedReturnLine(command, line, amounts));
        }

        var previousReturned = returns.Sum(item => item.TotalReturnAmount);
        ReturnFinancialAmounts financials;
        try
        {
            financials = ReturnCalculations.CalculateFinancials(
                sale.TotalAmount, sale.PaidAmount, previousReturned, returns.Sum(item => item.RefundAmount),
                calculatedLines.Sum(item => item.Amounts.ReturnLineAmount));
        }
        catch (DomainRuleException exception)
        {
            throw new ApplicationConflictException(exception.Code, exception.Message);
        }
        return new ReturnCalculation(sale, calculatedLines, previousReturned, financials);
    }

    public static string CreateFingerprint(CreateReturnCommand command, PaymentMethod? method)
    {
        var lines = command.Lines.OrderBy(item => item.OriginalSaleLineId).Select(item => string.Create(
            CultureInfo.InvariantCulture,
            $"{item.OriginalSaleLineId:N}:{item.Quantity:G29}:{item.Restock!.Value}"));
        var normalized = $"return|sale:{command.OriginalSaleId:N}|lines:{string.Join(';', lines)}|refund:{method?.ToString() ?? "null"}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    public static ReturnResult ToResult(CustomerReturn item, bool wasAlreadyCompleted) => new(
        item.Id,
        item.OriginalSaleId,
        item.Status.ToString(),
        item.Lines.Select(line => new ReturnLineResult(
            line.Id, line.OriginalSaleLineId, line.ProductId, line.Quantity, line.Restock,
            line.UnitSalePriceBasis, line.ReturnLineAmount, line.UnitCostBasis, line.RestockedInventoryValue)).ToArray(),
        item.RefundPayments.Select(payment => new ReturnRefundPaymentResult(
            payment.Id, payment.Amount, payment.Method.ToString(), payment.OccurredAt)).ToArray(),
        item.TotalReturnAmount,
        item.RefundAmount,
        item.CompletedByUserId,
        item.CreatedAt,
        item.CompletedAt,
        wasAlreadyCompleted);
}

internal sealed record CalculatedReturnLine(ReturnLineCommand Command, SaleLine SaleLine, ReturnLineAmounts Amounts);
internal sealed record ReturnCalculation(
    Sale Sale,
    IReadOnlyList<CalculatedReturnLine> Lines,
    decimal PreviousReturnedValue,
    ReturnFinancialAmounts Financials)
{
    public ReturnPreviewResult ToPreview() => new(
        Sale.Id,
        Lines.Select(item => new ReturnPreviewLineResult(
            item.SaleLine.Id, item.SaleLine.ProductId, item.Command.Quantity, item.Command.Restock!.Value,
            item.SaleLine.Quantity - item.Amounts.RemainingQuantityBefore,
            item.Amounts.RemainingQuantityBefore,
            item.Amounts.ReturnLineAmount,
            item.Amounts.RestockedInventoryValue)).ToArray(),
        Lines.Sum(item => item.Amounts.ReturnLineAmount),
        PreviousReturnedValue,
        Financials.CumulativeReturnedValue,
        Financials.NetSaleObligation,
        Financials.NetCashHeld,
        Financials.Outstanding,
        Financials.RefundDueNow,
        Financials.RefundDueNow > 0);
}
