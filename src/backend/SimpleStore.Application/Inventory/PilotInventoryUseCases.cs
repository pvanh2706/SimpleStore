using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Stores;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Operations;

namespace SimpleStore.Application.Inventory;

public sealed class CreateStockAdjustmentUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    IPilotReadinessRepository repository,
    TimeProvider timeProvider)
{
    public async Task<StockAdjustmentResult> ExecuteAsync(
        CreateStockAdjustmentCommand command,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserGuard.GetRequiredUserId(currentUser);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(currentUser, slice1Repository, cancellationToken);
        var fingerprint = Fingerprint.Create(
            command.ProductId,
            command.QuantityDelta,
            command.AdjustmentUnitCost,
            command.Reason?.Trim());

        return await repository.ExecuteInTransactionAsync(async token =>
        {
            await repository.AcquireOperationLockAsync(command.OperationId, token);
            var existingOperation = await repository.GetOperationAsync(storeId, command.OperationId, token);
            if (existingOperation is not null)
            {
                EnsureRetry(existingOperation, BusinessOperationTypes.AdjustStock, fingerprint);
                var existing = await repository.GetStockAdjustmentAsync(
                    storeId,
                    existingOperation.ResultReference!.Value,
                    token) ?? throw new ApplicationConflictException(
                        "operation-result-missing",
                        "The completed adjustment result is unavailable.");
                return ToResult(existing, true);
            }

            var product = await repository.GetProductAsync(storeId, command.ProductId, token)
                ?? throw new ApplicationNotFoundException("product-not-found", "Product was not found.");
            if (!product.IsActive)
            {
                throw new ApplicationConflictException("product-inactive", "Inactive products cannot be adjusted.");
            }

            var warehouse = await repository.GetMainWarehouseAsync(storeId, token)
                ?? throw new ApplicationNotFoundException("main-warehouse-not-found", "Main warehouse was not found.");
            var balances = await repository.LockInventoryBalancesAsync(storeId, warehouse.Id, [product.Id], token);
            if (!balances.TryGetValue(product.Id, out var balance))
            {
                throw new ApplicationNotFoundException("inventory-balance-not-found", "Inventory balance was not found.");
            }

            var cost = InventoryAdjustmentCostResolver.Resolve(
                balance,
                command.QuantityDelta,
                command.AdjustmentUnitCost,
                product.ReferencePurchaseCost);
            var quantityBefore = balance.QuantityOnHand;
            var valueBefore = balance.InventoryValue;
            var now = timeProvider.GetUtcNow();
            balance.ApplyInventoryAdjustment(
                command.QuantityDelta,
                cost.InventoryValueDelta,
                cost.UnitCost,
                cost.EstablishesReliableBasis,
                now);
            var adjustment = StockAdjustment.Create(
                command.OperationId,
                storeId,
                warehouse.Id,
                product.Id,
                command.QuantityDelta,
                command.AdjustmentUnitCost,
                cost,
                command.Reason ?? string.Empty,
                quantityBefore,
                balance.QuantityOnHand,
                valueBefore,
                balance.InventoryValue,
                balance.AverageCost,
                balance.HasAverageCost,
                userId,
                now);
            var movement = InventoryMovement.CreateAdjustment(
                storeId,
                warehouse.Id,
                product.Id,
                command.QuantityDelta,
                cost.InventoryValueDelta,
                cost.UnitCost,
                cost.Reliability,
                adjustment.Reason,
                adjustment.Id,
                userId,
                now);
            adjustment.RecordMovement(movement.Id);
            repository.AddStockAdjustment(adjustment);
            repository.AddInventoryMovement(movement);
            repository.AddBusinessOperation(BusinessOperation.AdjustStock(
                command.OperationId, storeId, fingerprint, adjustment.Id, now));
            return ToResult(adjustment, false);
        }, cancellationToken);
    }

    private static StockAdjustmentResult ToResult(StockAdjustment item, bool retry) =>
        new(
            item.Id, item.OperationId, item.ProductId, item.QuantityDelta,
            item.AdjustmentUnitCost, item.EffectiveUnitCost, item.CostReliability.ToString(),
            item.InventoryValueDelta, item.Reason, item.QuantityBefore, item.QuantityAfter,
            item.InventoryValueBefore, item.InventoryValueAfter, item.AverageCostAfter,
            item.HasAverageCostAfter, item.InventoryMovementId, item.OccurredAt, retry);

    internal static void EnsureRetry(BusinessOperation operation, string type, string fingerprint)
    {
        if (operation.OperationType != type || operation.RequestFingerprint != fingerprint)
        {
            throw new ApplicationConflictException(
                "operation-intent-conflict",
                "OperationId was already used for a different request.");
        }
    }
}

public sealed class GetStocktakeContextUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    IPilotReadinessRepository repository)
{
    public async Task<StocktakeContextResult> ExecuteAsync(Guid productId, CancellationToken cancellationToken)
    {
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(currentUser, slice1Repository, cancellationToken);
        var product = await repository.GetProductAsync(storeId, productId, cancellationToken)
            ?? throw new ApplicationNotFoundException("product-not-found", "Product was not found.");
        if (!product.IsActive)
        {
            throw new ApplicationConflictException("product-inactive", "Inactive products cannot be counted.");
        }

        var warehouse = await repository.GetMainWarehouseAsync(storeId, cancellationToken)
            ?? throw new ApplicationNotFoundException("main-warehouse-not-found", "Main warehouse was not found.");
        var balance = await repository.GetInventoryBalanceAsync(storeId, warehouse.Id, productId, cancellationToken)
            ?? throw new ApplicationNotFoundException("inventory-balance-not-found", "Inventory balance was not found.");
        return new StocktakeContextResult(
            product.Id,
            product.Name,
            product.Sku,
            product.Unit,
            balance.QuantityOnHand,
            Convert.ToBase64String(balance.RowVersion),
            balance.HasAverageCost,
            balance.AverageCost);
    }
}

public sealed class SubmitStocktakeUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    IPilotReadinessRepository repository,
    TimeProvider timeProvider)
{
    public async Task<StocktakeResultDto> ExecuteAsync(
        SubmitStocktakeCommand command,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserGuard.GetRequiredUserId(currentUser);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(currentUser, slice1Repository, cancellationToken);
        var expectedRevision = ParseRevision(command.ExpectedRevision);
        var fingerprint = Fingerprint.Create(
            command.ProductId,
            command.ExpectedQuantity,
            command.ExpectedRevision,
            command.CountedQuantity,
            command.AdjustmentUnitCost,
            command.Note?.Trim());

        return await repository.ExecuteInTransactionAsync(async token =>
        {
            await repository.AcquireOperationLockAsync(command.OperationId, token);
            var existingOperation = await repository.GetOperationAsync(storeId, command.OperationId, token);
            if (existingOperation is not null)
            {
                CreateStockAdjustmentUseCase.EnsureRetry(
                    existingOperation,
                    BusinessOperationTypes.RecordStocktake,
                    fingerprint);
                var existing = await repository.GetStocktakeAsync(
                    storeId,
                    existingOperation.ResultReference!.Value,
                    token) ?? throw new ApplicationConflictException(
                        "operation-result-missing",
                        "The completed stocktake result is unavailable.");
                return ToResult(existing, true);
            }

            var product = await repository.GetProductAsync(storeId, command.ProductId, token)
                ?? throw new ApplicationNotFoundException("product-not-found", "Product was not found.");
            if (!product.IsActive)
            {
                throw new ApplicationConflictException("product-inactive", "Inactive products cannot be counted.");
            }

            var warehouse = await repository.GetMainWarehouseAsync(storeId, token)
                ?? throw new ApplicationNotFoundException("main-warehouse-not-found", "Main warehouse was not found.");
            var balances = await repository.LockInventoryBalancesAsync(storeId, warehouse.Id, [product.Id], token);
            if (!balances.TryGetValue(product.Id, out var balance))
            {
                throw new ApplicationNotFoundException("inventory-balance-not-found", "Inventory balance was not found.");
            }

            if (balance.QuantityOnHand != command.ExpectedQuantity
                || !balance.RowVersion.SequenceEqual(expectedRevision))
            {
                throw new ApplicationConflictException(
                    "stocktake-stale",
                    "Inventory changed during counting. Refresh and recount before submitting a new stocktake.",
                    new Dictionary<string, object?>
                    {
                        ["expectedQuantity"] = command.ExpectedQuantity,
                        ["expectedRevision"] = command.ExpectedRevision,
                        ["currentQuantity"] = balance.QuantityOnHand,
                        ["currentRevision"] = Convert.ToBase64String(balance.RowVersion)
                    });
            }

            var difference = command.CountedQuantity - command.ExpectedQuantity;
            var cost = difference == 0
                ? null
                : InventoryAdjustmentCostResolver.Resolve(
                    balance,
                    difference,
                    command.AdjustmentUnitCost,
                    product.ReferencePurchaseCost);
            if (difference == 0 && command.AdjustmentUnitCost.HasValue)
            {
                throw new Domain.DomainRuleException(
                    "adjustment-unit-cost-not-allowed",
                    "Adjustment unit cost is not accepted for a zero-difference stocktake.");
            }

            var valueBefore = balance.InventoryValue;
            var now = timeProvider.GetUtcNow();
            if (cost is not null)
            {
                balance.ApplyInventoryAdjustment(
                    difference,
                    cost.InventoryValueDelta,
                    cost.UnitCost,
                    cost.EstablishesReliableBasis,
                    now);
            }

            var stocktake = StocktakeResult.Create(
                command.OperationId,
                storeId,
                warehouse.Id,
                product.Id,
                command.ExpectedQuantity,
                expectedRevision,
                command.CountedQuantity,
                command.AdjustmentUnitCost,
                cost,
                command.Note,
                valueBefore,
                balance.InventoryValue,
                balance.AverageCost,
                balance.HasAverageCost,
                userId,
                now);
            if (cost is not null)
            {
                var movement = InventoryMovement.CreateStocktakeAdjustment(
                    storeId,
                    warehouse.Id,
                    product.Id,
                    difference,
                    cost.InventoryValueDelta,
                    cost.UnitCost,
                    cost.Reliability,
                    stocktake.Note,
                    command.ExpectedQuantity,
                    command.CountedQuantity,
                    stocktake.Id,
                    userId,
                    now);
                stocktake.RecordMovement(movement.Id);
                repository.AddInventoryMovement(movement);
            }

            repository.AddStocktake(stocktake);
            repository.AddBusinessOperation(BusinessOperation.RecordStocktake(
                command.OperationId, storeId, fingerprint, stocktake.Id, now));
            return ToResult(stocktake, false);
        }, cancellationToken);
    }

    private static byte[] ParseRevision(string revision)
    {
        try
        {
            var parsed = Convert.FromBase64String(revision ?? string.Empty);
            if (parsed.Length == 8)
            {
                return parsed;
            }
        }
        catch (FormatException)
        {
        }

        throw new Domain.DomainRuleException(
            "invalid-stocktake-revision",
            "Stocktake revision is invalid.");
    }

    private static StocktakeResultDto ToResult(StocktakeResult item, bool retry) =>
        new(
            item.Id, item.OperationId, item.ProductId, item.ExpectedQuantity,
            Convert.ToBase64String(item.ExpectedBalanceRowVersion), item.CountedQuantity,
            item.Difference, item.AdjustmentUnitCost, item.EffectiveUnitCost,
            item.CostReliability.ToString(), item.InventoryValueDelta, item.Note,
            item.InventoryValueBefore, item.InventoryValueAfter, item.AverageCostAfter,
            item.HasAverageCostAfter, item.InventoryMovementId, item.OccurredAt, retry);
}

internal static class Fingerprint
{
    public static string Create(params object?[] values)
    {
        var canonical = string.Join('|', values.Select(value => value switch
        {
            null => "<null>",
            decimal number => number.ToString("0.############################", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        }));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
