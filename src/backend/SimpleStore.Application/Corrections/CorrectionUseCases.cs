using System.Security.Cryptography;
using System.Text;
using SimpleStore.Application.Abstractions;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Stores;
using SimpleStore.Application.Suppliers;
using SimpleStore.Domain;
using SimpleStore.Domain.Corrections;
using SimpleStore.Domain.Inventory;
using SimpleStore.Domain.Operations;
using SimpleStore.Domain.Purchases;

namespace SimpleStore.Application.Corrections;

public sealed class VoidSaleUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice4Repository repository,
    TimeProvider timeProvider)
{
    public async Task<SaleVoidResult> ExecuteAsync(Guid saleId, VoidTransactionCommand command, CancellationToken cancellationToken)
    {
        var userId = CurrentUserGuard.GetRequiredUserId(currentUser);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(currentUser, slice1Repository, cancellationToken);
        var reason = SaleVoid.NormalizeReason(command.Reason);
        var fingerprint = Fingerprints.Void("sale-void", saleId, reason);
        return await repository.ExecuteInTransactionAsync(async token =>
        {
            await repository.AcquireOperationLockAsync(command.OperationId, token);
            var existingOperation = await repository.GetOperationAsync(storeId, command.OperationId, token);
            if (existingOperation is not null)
            {
                if (existingOperation.OperationType != BusinessOperationTypes.VoidSale
                    || existingOperation.RequestFingerprint != fingerprint)
                    throw new ApplicationConflictException("idempotency-key-reused", "OperationId was already used for a different request.");
                var existingVoid = await repository.GetSaleVoidAsync(storeId, saleId, token);
                if (existingVoid is null || existingVoid.Id != existingOperation.ResultReference)
                    throw new ApplicationConflictException("sale-void-result-missing", "Completed sale void result was not found.");
                return ToResult(existingVoid, true);
            }

            await repository.AcquireSaleCorrectionLockAsync(saleId, token);
            var sale = await repository.GetSaleAsync(storeId, saleId, token)
                ?? throw new ApplicationNotFoundException("sale-not-found", "Sale was not found.");
            if (await repository.GetSaleVoidAsync(storeId, saleId, token) is not null)
                throw new ApplicationConflictException("sale-already-voided", "Sale is already voided.");
            if ((await repository.GetReturnsForSaleAsync(storeId, saleId, token)).Count > 0)
                throw new ApplicationConflictException("sale-has-returns", "A sale with completed returns cannot be voided.");

            var productIds = sale.Lines.Select(item => item.ProductId).Distinct().OrderBy(item => item).ToArray();
            var balances = await repository.LockInventoryBalancesAsync(storeId, sale.WarehouseId, productIds, token);
            if (balances.Count != productIds.Length)
                throw new ApplicationConflictException("inventory-balance-missing", "An inventory balance is missing for a sale product.");
            var movements = await repository.GetSaleMovementsAsync(storeId, sale.Lines.Select(item => item.Id).ToArray(), token);
            var now = timeProvider.GetUtcNow();
            var saleVoid = SaleVoid.Create(storeId, saleId, reason, userId, now);
            foreach (var line in sale.Lines.OrderBy(item => item.ProductId))
            {
                if (!movements.TryGetValue(line.Id, out var original)
                    || original.WarehouseId != sale.WarehouseId || original.ProductId != line.ProductId
                    || original.QuantityDelta != -line.Quantity || original.SourceType != "SaleLine")
                    throw new ApplicationConflictException("sale-void-inventory-evidence-invalid", "Original sale inventory evidence is invalid.");
                var expectedValue = Math.Round(line.Quantity * line.UnitCostAtSale, 2, MidpointRounding.AwayFromZero);
                if (original.InventoryValueDelta != -expectedValue)
                    throw new ApplicationConflictException("sale-void-inventory-evidence-invalid", "Original sale inventory value evidence is invalid.");
                var inventoryValue = Math.Abs(original.InventoryValueDelta);
                balances[line.ProductId].ReceiveInbound(line.Quantity, inventoryValue, now);
                repository.AddInventoryMovement(InventoryMovement.CreateSaleVoid(
                    storeId, sale.WarehouseId, line.ProductId, line.Quantity, inventoryValue,
                    line.UnitCostAtSale, saleVoid.Id, userId, now));
            }
            repository.AddSaleVoid(saleVoid);
            repository.AddBusinessOperation(BusinessOperation.VoidSale(
                command.OperationId, storeId, fingerprint, saleVoid.Id, now));
            return ToResult(saleVoid, false);
        }, cancellationToken);
    }

    private static SaleVoidResult ToResult(SaleVoid item, bool retry) =>
        new(item.Id, item.OriginalSaleId, item.Reason, item.VoidedByUserId, item.VoidedAt, retry);
}

public sealed class VoidPurchaseUseCase(
    ICurrentUser currentUser,
    ISlice1Repository slice1Repository,
    ISlice4Repository repository,
    TimeProvider timeProvider)
{
    public async Task<PurchaseVoidResult> ExecuteAsync(Guid purchaseId, VoidTransactionCommand command, CancellationToken cancellationToken)
    {
        var userId = CurrentUserGuard.GetRequiredUserId(currentUser);
        var storeId = await CurrentUserGuard.GetRequiredStoreIdAsync(currentUser, slice1Repository, cancellationToken);
        var reason = SaleVoid.NormalizeReason(command.Reason);
        var fingerprint = Fingerprints.Void("purchase-void", purchaseId, reason);
        return await repository.ExecuteInTransactionAsync(async token =>
        {
            await repository.AcquireOperationLockAsync(command.OperationId, token);
            var existingOperation = await repository.GetOperationAsync(storeId, command.OperationId, token);
            if (existingOperation is not null)
            {
                if (existingOperation.OperationType != BusinessOperationTypes.VoidPurchase
                    || existingOperation.RequestFingerprint != fingerprint)
                    throw new ApplicationConflictException("idempotency-key-reused", "OperationId was already used for a different request.");
                var existingVoid = await repository.GetPurchaseVoidAsync(storeId, purchaseId, token);
                if (existingVoid is null || existingVoid.Id != existingOperation.ResultReference)
                    throw new ApplicationConflictException("purchase-void-result-missing", "Completed purchase void result was not found.");
                return ToResult(existingVoid, true);
            }

            await repository.AcquirePurchaseCorrectionLockAsync(purchaseId, token);
            var purchase = await repository.GetPurchaseAsync(storeId, purchaseId, token)
                ?? throw new ApplicationNotFoundException("purchase-not-found", "Purchase was not found.");
            if (purchase.Status != PurchaseStatus.Completed)
                throw new ApplicationConflictException("purchase-not-completed", "Only a completed purchase can be voided.");
            if (await repository.GetPurchaseVoidAsync(storeId, purchaseId, token) is not null)
                throw new ApplicationConflictException("purchase-already-voided", "Purchase is already voided.");

            var bases = await repository.GetPurchaseReversalBasesAsync(storeId, purchaseId, token);
            if (bases.Count != purchase.Lines.Count)
                throw new ApplicationConflictException("purchase-void-reversal-basis-unavailable", "Purchase reversal basis is unavailable.");
            var basisByLine = bases.ToDictionary(item => item.PurchaseLineId);
            if (purchase.Lines.Any(item => !basisByLine.ContainsKey(item.Id)))
                throw new ApplicationConflictException("purchase-void-reversal-basis-unavailable", "Purchase reversal basis is unavailable.");

            var productIds = purchase.Lines.Select(item => item.ProductId).OrderBy(item => item).ToArray();
            var firstBasis = bases[0];
            var balances = await repository.LockInventoryBalancesAsync(storeId, firstBasis.WarehouseId, productIds, token);
            var products = await repository.GetProductsForUpdateAsync(storeId, productIds, token);
            if (balances.Count != productIds.Length || products.Count != productIds.Length)
                throw new ApplicationConflictException("purchase-void-reversal-basis-invalid", "Purchase reversal state is incomplete.");

            var validated = new List<(PurchaseLine Line, PurchaseLineReversalBasis Basis, InventoryMovement Movement)>();
            foreach (var line in purchase.Lines.OrderBy(item => item.ProductId))
            {
                var basis = basisByLine[line.Id];
                var movement = await repository.GetInventoryMovementAsync(storeId, basis.PurchaseMovementId, token);
                if (basis.PurchaseId != purchase.Id || basis.ProductId != line.ProductId
                    || basis.WarehouseId != firstBasis.WarehouseId || movement is null
                    || movement.MovementType != InventoryMovementType.Purchase || movement.SourceType != "PurchaseLine"
                    || movement.SourceId != line.Id || movement.ProductId != line.ProductId
                    || movement.WarehouseId != basis.WarehouseId || movement.QuantityDelta != line.Quantity
                    || movement.InventoryValueDelta != line.LineAmount)
                    throw new ApplicationConflictException("purchase-void-reversal-basis-invalid", "Purchase reversal basis or movement is invalid.");
                if (await repository.HasLaterInventoryMovementAsync(
                    storeId, basis.WarehouseId, line.ProductId, movement.LedgerSequence, token))
                    throw new ApplicationConflictException("purchase-void-downstream-inventory-dependency", "A later inventory movement prevents direct purchase void.");

                var balance = balances[line.ProductId];
                var expectedQuantity = basis.QuantityBefore + line.Quantity;
                var expectedValue = basis.InventoryValueBefore + line.LineAmount;
                var expectedAverage = basis.AverageCostBefore;
                var expectedHasAverage = basis.HasAverageCostBefore;
                if (expectedQuantity > 0 && expectedValue >= 0)
                {
                    expectedAverage = Math.Round(expectedValue / expectedQuantity, 4, MidpointRounding.AwayFromZero);
                    expectedHasAverage = true;
                }
                else if (expectedQuantity > 0)
                {
                    expectedHasAverage = false;
                }
                if (balance.QuantityOnHand != expectedQuantity || balance.InventoryValue != expectedValue
                    || balance.AverageCost != expectedAverage || balance.HasAverageCost != expectedHasAverage)
                    throw new ApplicationConflictException("purchase-void-downstream-inventory-dependency", "Current inventory state does not match the reversible purchase state.");
                var product = products[line.ProductId];
                if (product.ReferencePurchaseCost != basis.ReferencePurchaseCostApplied
                    || product.ReferencePurchaseCostRevision != basis.ReferencePurchaseCostRevisionAfterPurchase)
                    throw new ApplicationConflictException("purchase-void-reference-cost-dependency", "A later reference purchase cost mutation prevents direct purchase void.");
                validated.Add((line, basis, movement!));
            }

            var now = timeProvider.GetUtcNow();
            var purchaseVoid = PurchaseVoid.Create(storeId, purchaseId, reason, userId, now);
            foreach (var item in validated)
            {
                var balance = balances[item.Line.ProductId];
                var quantityDelta = item.Basis.QuantityBefore - balance.QuantityOnHand;
                var valueDelta = item.Basis.InventoryValueBefore - balance.InventoryValue;
                repository.AddInventoryMovement(InventoryMovement.CreatePurchaseVoid(
                    storeId, item.Basis.WarehouseId, item.Line.ProductId, quantityDelta, valueDelta,
                    item.Line.UnitPrice, purchaseVoid.Id, userId, now));
                balance.RestoreExact(
                    item.Basis.QuantityBefore, item.Basis.InventoryValueBefore, item.Basis.AverageCostBefore,
                    item.Basis.HasAverageCostBefore, now);
                products[item.Line.ProductId].RestoreReferencePurchaseCost(item.Basis.ReferencePurchaseCostBefore, now);
            }
            repository.AddPurchaseVoid(purchaseVoid);
            repository.AddBusinessOperation(BusinessOperation.VoidPurchase(
                command.OperationId, storeId, fingerprint, purchaseVoid.Id, now));
            return ToResult(purchaseVoid, false);
        }, cancellationToken);
    }

    private static PurchaseVoidResult ToResult(PurchaseVoid item, bool retry) =>
        new(item.Id, item.OriginalPurchaseId, item.Reason, item.VoidedByUserId, item.VoidedAt, retry);
}

internal static class Fingerprints
{
    public static string Void(string type, Guid targetId, string reason)
    {
        var normalized = $"{type}|target:{targetId:N}|reason:{reason}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
